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
            if (CurrentHP <= 0)
                Die();
        }

        private void Die()
        {
            var gm = GameManager.Instance;
            if (gm != null) gm.Run.AddKill(ScoreValue);
            AudioManager.Hit();
            Died?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
