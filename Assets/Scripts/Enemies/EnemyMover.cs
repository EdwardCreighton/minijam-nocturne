using Nocturne.Core;
using UnityEngine;

namespace Nocturne.Enemies
{
    /// <summary>
    /// Simple top-down chase (TZ §6.1): idle until the player enters aggro radius,
    /// then MovePosition toward the player with steering separation from other
    /// enemies. No pathfinding — wall slide (MovementUtil) handles corners.
    /// Optional periodic dash (Dasher type): dashSpeedMult &gt; 1 enables it.
    /// Moves only while GameState is Playing.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class EnemyMover : MonoBehaviour
    {
        [Header("Dash (1 = off)")]
        public float dashSpeedMult = 1f;
        public float dashInterval = 3f;
        public float dashDuration = 0.4f;

        private Rigidbody2D rb;
        private Transform player;
        private float dashTimer;

        /// <summary>Ease-out rate of queued knockback (higher = snappier).</summary>
        private const float KnockbackDecay = 12f;

        /// <summary>Queued shove, consumed over several FixedUpdates (hit impact).</summary>
        private Vector2 knockbackRemaining;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            var playerGo = GameObject.FindWithTag("Player");
            if (playerGo != null) player = playerGo.transform;
        }

        /// <summary>
        /// Queues a smooth shove away from the attacker. Consumed ease-out over
        /// several FixedUpdates, merged with the chase delta into a single
        /// MovePosition per step (two MovePositions in one step would overwrite).
        /// Rapid hits stack up to <paramref name="maxTotal"/> world units.
        /// </summary>
        public void AddKnockback(Vector2 push, float maxTotal)
        {
            if (push.sqrMagnitude < 1e-8f) return;
            knockbackRemaining += push;
            var cap = Mathf.Max(push.magnitude, Mathf.Max(0f, maxTotal));
            if (knockbackRemaining.sqrMagnitude > cap * cap)
                knockbackRemaining = knockbackRemaining.normalized * cap;
        }

        private void FixedUpdate()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Playing || player == null) return;

            // Chase + knockback share one TryMove: a single MovePosition writer
            // per step, wall slide still applies to the combined delta.
            MovementUtil.TryMove(rb, ChaseDelta(gm) + ConsumeKnockback());
        }

        /// <summary>
        /// Chase step for this frame (zero outside aggro range).
        /// Holds a standoff at attack radius instead of entering the player's
        /// exact point; separation drift still applies up close so enemies
        /// spread around the player rather than stacking.
        /// </summary>
        private Vector2 ChaseDelta(GameManager gm)
        {
            var cfg = gm.config;
            var speed = cfg != null ? cfg.enemySpeed : 2.5f;
            var aggro = cfg != null ? cfg.enemyAggroRadius : 6f;
            var standoff = cfg != null ? cfg.enemyAttackRadius : 1f;

            var toPlayer = (Vector2)player.position - rb.position;
            var dist = toPlayer.magnitude;
            if (dist > aggro || dist < 0.0001f)
                return Vector2.zero;

            var chase = dist > standoff ? toPlayer / dist : Vector2.zero;
            var dir = chase + Separation() * 0.7f;
            if (dir.sqrMagnitude < 0.0001f) return Vector2.zero;

            var speedMult = 1f;
            if (dashSpeedMult > 1f)
            {
                dashTimer += Time.fixedDeltaTime;
                var cycle = Mathf.Max(0.1f, dashInterval) + Mathf.Max(0f, dashDuration);
                if (dashTimer >= cycle) dashTimer = 0f;
                if (dashTimer >= dashInterval) speedMult = dashSpeedMult;
            }

            return dir.normalized * speed * speedMult * Time.fixedDeltaTime;
        }

        /// <summary>Next ease-out slice of the queued shove (snaps under 1cm).</summary>
        private Vector2 ConsumeKnockback()
        {
            if (knockbackRemaining.sqrMagnitude < 1e-8f) return Vector2.zero;
            var step = knockbackRemaining * Mathf.Min(1f, KnockbackDecay * Time.fixedDeltaTime);
            knockbackRemaining -= step;
            if (knockbackRemaining.sqrMagnitude < 0.0001f)
                knockbackRemaining = Vector2.zero;
            return step;
        }

        private Vector2 Separation()
        {
            var push = Vector2.zero;
            var neighbors = Physics2D.OverlapCircleAll(transform.position, 0.8f, 1 << Layers.Enemy);
            foreach (var n in neighbors)
            {
                if (n.gameObject == gameObject) continue;
                var away = (Vector2)transform.position - (Vector2)n.transform.position;
                if (away.sqrMagnitude > 0.0001f)
                    push += away.normalized / Mathf.Max(0.2f, away.magnitude);
            }

            return push;
        }
    }
}
