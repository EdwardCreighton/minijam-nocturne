using UnityEngine;

namespace Nocturne.World
{
    /// <summary>
    /// One of the alternative run-ending points (TZ §4.1, §10).
    /// Trigger collider (added by setup); reaching any one ends the run.
    /// </summary>
    public sealed class FinishPoint : MonoBehaviour
    {
        [Tooltip("Stable id reported to RunState.RegisterWin.")]
        public string finishId = "finish_a";

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            var gm = Core.GameManager.Instance;
            if (gm != null) gm.OnFinishReached(finishId);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.7f);
        }
    }
}
