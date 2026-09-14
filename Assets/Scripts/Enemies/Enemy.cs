using Nocturne.Combat;
using Nocturne.Core;
using UnityEngine;

namespace Nocturne.Enemies
{
    /// <summary>
    /// Enemy body: HP, contact damage value, kill score (TZ §6.1).
    /// Base stats are per-prefab fields (TZ §11: balance via SO + prefab fields);
    /// difficulty multipliers are injected by the spawner at spawn time (P5);
    /// live enemies are never re-scaled. Death pays score via RunState.AddKill.
    /// </summary>
    public sealed class Enemy : MonoBehaviour, IDamageable
    {
        [Header("Base stats (per type, tuned per prefab)")]
        public int baseHP = 50;
        public int baseDamage = 10;
        public int baseScore = 10;

        public int ScoreValue { get; private set; }
        public int Damage { get; private set; }
        public int CurrentHP { get; private set; }
        public bool IsAlive => CurrentHP > 0;

        public event System.Action<Enemy> Died;

        private Rigidbody2D rb;
        private EnemyMover mover;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            mover = GetComponent<EnemyMover>();
            // Code-driven visuals need no prefab setup: ensure the shared
            // MainCharacter animation set is actually driven on this enemy.
            if (GetComponent<EnemyVisual>() == null)
                gameObject.AddComponent<EnemyVisual>();
        }

        /// <summary>Called once by the spawner right after instantiation.</summary>
        public void Initialize(float hpMult, float dmgMult)
        {
            ScoreValue = Mathf.Max(0, baseScore);
            Damage = Mathf.Max(1, Mathf.RoundToInt(baseDamage * Mathf.Max(1f, dmgMult)));
            CurrentHP = Mathf.Max(1, Mathf.RoundToInt(baseHP * Mathf.Max(1f, hpMult)));
        }

        public void TakeDamage(int amount, Vector2 sourcePosition)
        {
            if (!IsAlive || amount <= 0) return;
            CurrentHP = Mathf.Max(0, CurrentHP - amount);
            AudioManager.Hit();
            if (CurrentHP <= 0)
            {
                Die();
                return;
            }
            ApplyKnockback(sourcePosition);
        }

        /// <summary>
        /// Smooth push away from the attacker (hit impact), consumed ease-out
        /// by EnemyMover over several FixedUpdates. Dead enemies are skipped
        /// (they are destroyed anyway).
        /// </summary>
        private void ApplyKnockback(Vector2 sourcePosition)
        {
            var gm = GameManager.Instance;
            var cfg = gm != null ? gm.config : null;
            var distance = cfg != null ? cfg.knockbackDistance : 0.5f;
            if (distance <= 0f) return;
            var away = (Vector2)transform.position - sourcePosition;
            if (away.sqrMagnitude < 0.0001f)
                away = Random.insideUnitCircle;
            if (away.sqrMagnitude < 0.0001f) return;
            var push = away.normalized * distance;
            if (mover == null) mover = GetComponent<EnemyMover>();
            if (mover != null)
            {
                mover.AddKnockback(push, distance * 2f);
                return;
            }
            // Misconfigured enemy (no mover): instant swept shove so the hit
            // still has impact instead of silently doing nothing.
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            if (rb != null) MovementUtil.TryKnockback(rb, push);
        }

        private void Die()
        {
            var gm = GameManager.Instance;
            if (gm != null) gm.Run.AddKill(ScoreValue);
            Died?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
