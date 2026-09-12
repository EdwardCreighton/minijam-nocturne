using UnityEngine;

namespace Nocturne.Core
{
    /// <summary>
    /// Top-down follow camera with instant snap (used on respawn so the view
    /// never lerps through walls). No smoothing in MVP.
    /// </summary>
    public sealed class FollowCam : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new(0f, 0f, -10f);

        private void LateUpdate()
        {
            if (target == null) return;
            transform.position = target.position + offset;
        }

        public void Snap()
        {
            if (target == null) return;
            transform.position = target.position + offset;
        }
    }
}
