using UnityEngine;

namespace Nocturne.World
{
    /// <summary>
    /// Attempt-local state vs run-persistent RunState (TZ §9).
    /// P1: captures the StartPoint. P2/P3: player reposition + enemy reset hooks.
    /// </summary>
    public sealed class AttemptResetter : MonoBehaviour
    {
        public StartPoint startPoint;

        private void Awake()
        {
            if (startPoint == null)
                startPoint = FindFirstObjectByType<StartPoint>();
        }

        public Vector3 RespawnPosition() =>
            startPoint != null ? startPoint.transform.position : Vector3.zero;
    }
}
