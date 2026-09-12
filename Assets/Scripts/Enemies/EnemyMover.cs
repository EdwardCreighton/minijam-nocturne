using Nocturne.Core;
using UnityEngine;

namespace Nocturne.Enemies
{
    /// <summary>
    /// Simple top-down chase (TZ §6.1): idle until the player enters aggro radius,
    /// then MovePosition toward the player with steering separation from other
    /// enemies. No pathfinding — physics slide handles corners.
    /// Moves only while GameState is Playing.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class EnemyMover : MonoBehaviour
    {
        private Rigidbody2D rb;
        private Transform player;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            var playerGo = GameObject.FindWithTag("Player");
            if (playerGo != null) player = playerGo.transform;
        }

        private void FixedUpdate()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Playing || player == null) return;

            var cfg = gm.config;
            var speed = cfg != null ? cfg.enemySpeed : 2.5f;
            var aggro = cfg != null ? cfg.enemyAggroRadius : 6f;

            var toPlayer = (Vector2)player.position - rb.position;
            if (toPlayer.sqrMagnitude > aggro * aggro || toPlayer.sqrMagnitude < 0.0001f)
                return;

            var dir = toPlayer.normalized + Separation() * 0.7f;
            if (dir.sqrMagnitude < 0.0001f) return;
            MovementUtil.TryMove(rb, dir.normalized * speed * Time.fixedDeltaTime);
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
