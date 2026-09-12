using Nocturne.Core;
using UnityEngine;

namespace Nocturne.World
{
    /// <summary>
    /// A проход (door/portal/barrier): unique stable id, point cost, permanent openness (TZ §7).
    /// P1: data + collider toggle + self-registration. Hold/spend/prompt land in P4.
    /// </summary>
    public sealed class Gate : MonoBehaviour
    {
        [Tooltip("Unique stable id, NOT tied to the GameObject name.")]
        public string id = "gate_01";
        [Min(1)] public int cost = 30;
        public bool isOpen;

        private Collider2D blocker;

        private void Awake()
        {
            blocker = GetComponent<Collider2D>();
            ApplyCollision();
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

        public void SetOpen(bool open)
        {
            isOpen = open;
            ApplyCollision();
            // TODO P4: swap visual/animation for open state.
        }

        private void ApplyCollision()
        {
            if (blocker != null)
                blocker.enabled = !isOpen;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = isOpen ? Color.gray : Color.red;
            Gizmos.DrawWireCube(transform.position, GetComponent<Collider2D>() is BoxCollider2D box
                ? (Vector3)box.size
                : new Vector3(1f, 3f, 0f));
        }
    }
}
