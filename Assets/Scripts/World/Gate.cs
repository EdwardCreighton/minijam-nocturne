using Nocturne.Core;
using UnityEngine;

namespace Nocturne.World
{
    /// <summary>
    /// A проход (door/portal/barrier): unique stable id, point cost, permanent openness (TZ §7).
    /// Hold/spend is driven by PlayerInteractor; this owns data, collision and visuals.
    /// </summary>
    public sealed class Gate : MonoBehaviour
    {
        [Tooltip("Unique stable id, NOT tied to the GameObject name.")]
        public string id = "gate_01";
        [Min(1)] public int cost = 30;
        public bool isOpen;

        public event System.Action<Gate> Opened;

        private Collider2D blocker;
        private SpriteRenderer visual;

        private void Awake()
        {
            blocker = GetComponent<Collider2D>();
            visual = GetComponent<SpriteRenderer>();
            ApplyCollision();
            ApplyVisual();
        }

        private void Start()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.RegisterGate(this);
        }

        private void OnValidate()
        {
            if (cost < 1) cost = 1;
            if (string.IsNullOrEmpty(id)) id = "gate_01";
            if (blocker == null) blocker = GetComponent<Collider2D>();
            ApplyCollision();
        }

        public bool CanOpen(int unspent) => !isOpen && unspent >= cost;

        /// <summary>Atomic spend + open. Returns false if funds/conditions changed mid-hold.</summary>
        public bool TryOpen(RunState run)
        {
            if (run == null || !CanOpen(run.Unspent)) return false;
            if (!run.TrySpend(cost, id)) return false;
            SetOpen(true);
            Opened?.Invoke(this);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayGateOpen();
            return true;
        }

        public void SetOpen(bool open)
        {
            isOpen = open;
            ApplyCollision();
            ApplyVisual();
        }

        private void ApplyCollision()
        {
            if (blocker != null)
                blocker.enabled = !isOpen;
        }

        private void ApplyVisual()
        {
            if (visual == null) return;
            // Placeholder: closed = solid blue, open = faded. Real art replaces colors, not logic.
            visual.color = isOpen ? new Color(0.3f, 1f, 0.3f, 0.25f) : new Color(0.3f, 0.5f, 1f, 1f);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = isOpen ? Color.gray : Color.red;
            Gizmos.DrawWireCube(transform.position, GetComponent<Collider2D>() is BoxCollider2D box
                ? (Vector3)box.size * 2
                : new Vector3(1f, 3f, 0f));
        }
    }
}
