using Nocturne.Core;
using Nocturne.World;
using UnityEngine;

namespace Nocturne.Player
{
    /// <summary>
    /// Gate proximity + hold-to-open (TZ §7.1). P2: nearest-gate scan + hold cancel
    /// (used by the death flow). Full hold/spend logic lands in P4.
    /// </summary>
    public sealed class PlayerInteractor : MonoBehaviour
    {
        private Gate[] gates = System.Array.Empty<Gate>();

        private void Awake()
        {
            gates = FindObjectsByType<Gate>(FindObjectsSortMode.None);
        }

        /// <summary>Nearest closed gate within interact radius, or null.</summary>
        public Gate NearestOpenableGate()
        {
            var gm = GameManager.Instance;
            if (gm == null) return null;
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

        /// <summary>Aborts an in-progress hold with no spend. Called on death/pause (P4: full use).</summary>
        public void CancelHold()
        {
            // TODO P4: reset hold progress + prompt here.
        }
    }
}
