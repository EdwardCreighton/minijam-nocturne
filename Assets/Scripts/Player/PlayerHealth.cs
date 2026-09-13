using Nocturne.Combat;
using Nocturne.Core;
using UnityEngine;

namespace Nocturne.Player
{
    /// <summary>
    /// Player HP with i-frame window (TZ §5.2). Death forwards to
    /// GameManager.OnPlayerDied; revival is done by the death flow (ResetHP).
    /// </summary>
    public sealed class PlayerHealth : MonoBehaviour, IDamageable
    {
        public int CurrentHP { get; private set; }
        public int MaxHP { get; private set; }
        public bool IsAlive => CurrentHP > 0;

        public event System.Action Died;
        public event System.Action HPChanged;
        /// <summary>Fired whenever damage is actually applied (also on lethal hits).</summary>
        public event System.Action Damaged;

        private float lastDamageTime = float.NegativeInfinity;

        // Initialized in Start: GameManager.Awake (which carries config) always runs first.
        private void Start()
        {
            var gm = GameManager.Instance;
            MaxHP = gm != null && gm.config != null ? gm.config.maxHP : 100;
            CurrentHP = MaxHP;
        }

        public void TakeDamage(int amount, Vector2 sourcePosition)
        {
            var gm = GameManager.Instance;
            if (!IsAlive || amount <= 0) return;
            if (gm != null && gm.State != GameState.Playing) return;

            var cooldown = gm != null && gm.config != null ? gm.config.damageCooldown : 0.5f;
            if (Time.time - lastDamageTime < cooldown) return;
            lastDamageTime = Time.time;

            CurrentHP = Mathf.Max(0, CurrentHP - amount);
            HPChanged?.Invoke();
            Damaged?.Invoke();

            if (CurrentHP <= 0)
            {
                Died?.Invoke();
                if (gm != null) gm.OnPlayerDied();
            }
        }

        /// <summary>Called by the death flow on respawn.</summary>
        public void ResetHP()
        {
            CurrentHP = MaxHP;
            lastDamageTime = float.NegativeInfinity;
            HPChanged?.Invoke();
        }
    }
}
