using Nocturne.Core;
using Nocturne.World;
using UnityEngine;

namespace Nocturne.Player
{
    /// <summary>
    /// Hold-to-open for gates (TZ §7.1). The ONLY spend path: while the Interact
    /// button is held, the player stands within radius of a closed affordable gate
    /// and the game is Playing, progress grows to holdTime, then Gate.TryOpen
    /// spends atomically. Any interruption resets progress with no spend:
    /// button released, out of radius, damage taken, death, pause.
    /// </summary>
    public sealed class PlayerInteractor : MonoBehaviour
    {
        /// <summary>Gate currently being held, or nearest in radius (0..1 progress).</summary>
        public Gate CurrentGate { get; private set; }
        public float HoldProgress { get; private set; }

        private Gate[] gates = System.Array.Empty<Gate>();
        private PlayerHealth health;
        private int lastHP;
        private float debounceUntil;

        private void Awake()
        {
            gates = FindObjectsByType<Gate>(FindObjectsSortMode.None);
            health = GetComponent<PlayerHealth>();
        }

        private void Start()
        {
            if (health != null) lastHP = health.CurrentHP;
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Playing || gm.Input == null)
            {
                ResetHold();
                return;
            }

            // Damage taken aborts the hold.
            if (health != null)
            {
                if (health.CurrentHP < lastHP)
                {
                    lastHP = health.CurrentHP;
                    ResetHold();
                    return;
                }

                lastHP = health.CurrentHP;
            }

            var gate = NearestClosedGateInRadius(gm);
            if (gate != CurrentGate) ResetHold();
            CurrentGate = gate;
            if (gate == null) return;

            // Any of these aborts/resets progress with no spend: released button,
            // debounce window, or insufficient funds (hold never starts unpaid).
            if (!gm.Input.Player.Interact.IsPressed()
                || Time.time < debounceUntil
                || !gate.CanOpen(gm.Run.Unspent))
            {
                HoldProgress = 0f;
                return;
            }

            var holdTime = gm.config != null ? gm.config.holdTime : 0.6f;
            HoldProgress += Time.deltaTime / Mathf.Max(0.05f, holdTime);
            if (HoldProgress < 1f) return;

            if (gate.TryOpen(gm.Run))
            {
                var debounce = gm.config != null ? gm.config.gateDebounce : 0.5f;
                debounceUntil = Time.time + Mathf.Max(0f, debounce);
            }

            ResetHold();
        }

        /// <summary>Nearest closed gate within interact radius, or null.</summary>
        public Gate NearestOpenableGate()
        {
            var gm = GameManager.Instance;
            return gm != null ? NearestClosedGateInRadius(gm) : null;
        }

        /// <summary>Aborts an in-progress hold with no spend. Called on death (P4: full use).</summary>
        public void CancelHold()
        {
            ResetHold();
        }

        private Gate NearestClosedGateInRadius(GameManager gm)
        {
            var radius = gm.config != null ? gm.config.interactRadius : 1.5f;
            Gate best = null;
            var bestDistSq = radius * radius;
            var pos = (Vector2)transform.position;
            foreach (var gate in gates)
            {
                if (gate == null || gate.isOpen) continue;
                var d = ((Vector2)gate.transform.position - pos).sqrMagnitude;
                if (d <= bestDistSq)
                {
                    bestDistSq = d;
                    best = gate;
                }
            }

            return best;
        }

        private void ResetHold()
        {
            CurrentGate = null;
            HoldProgress = 0f;
        }
    }
}
