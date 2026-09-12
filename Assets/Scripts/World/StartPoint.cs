using UnityEngine;

namespace Nocturne.World
{
    /// <summary>
    /// The single spawn point of GameLevel: run start + every respawn (TZ §4.1).
    /// Exactly one instance per level scene. Replaces the tag-based marker.
    /// </summary>
    public sealed class StartPoint : MonoBehaviour
    {
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
            Gizmos.DrawLine(transform.position + Vector3.down * 0.8f, transform.position + Vector3.up * 0.8f);
        }
    }
}
