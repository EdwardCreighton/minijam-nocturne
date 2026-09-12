using UnityEngine;

namespace Nocturne.World
{
    /// <summary>
    /// One of the alternative run-ending points (TZ §4.1, §10).
    /// Trigger detection + win flow land in P8; this only carries identity.
    /// </summary>
    public sealed class FinishPoint : MonoBehaviour
    {
        [Tooltip("Stable id reported to RunState.RegisterWin.")]
        public string finishId = "finish_a";

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.7f);
        }
    }
}
