using UnityEngine;

namespace Nocturne.Core
{
    /// <summary>
    /// Top-down follow camera with instant snap (used on respawn so the view
    /// never lerps through walls). No smoothing in MVP.
    /// Hit impact: <see cref="Shake"/> adds a short decaying random offset
    /// (called once per landing player swing).
    /// </summary>
    public sealed class FollowCam : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new(0f, 0f, -10f);

        private float shakeAmplitude;
        private float shakeDuration;
        private float shakeTimeLeft;

        private void LateUpdate()
        {
            if (target == null) return;
            transform.position = target.position + offset;
            if (shakeTimeLeft > 0f)
            {
                shakeTimeLeft -= Time.deltaTime;
                var t = shakeDuration > 0f ? Mathf.Clamp01(shakeTimeLeft / shakeDuration) : 0f;
                var rnd = Random.insideUnitCircle * (shakeAmplitude * t);
                transform.position += new Vector3(rnd.x, rnd.y, 0f);
                if (shakeTimeLeft <= 0f)
                {
                    shakeAmplitude = 0f;
                    shakeDuration = 0f;
                }
            }
        }

        public void Snap()
        {
            if (target == null) return;
            shakeAmplitude = 0f;
            shakeDuration = 0f;
            shakeTimeLeft = 0f;
            transform.position = target.position + offset;
        }

        /// <summary>Restarts the hit shake (overlapping shakes don't stack).</summary>
        public void Shake(float amplitude, float duration)
        {
            if (amplitude <= 0f || duration <= 0f) return;
            shakeAmplitude = amplitude;
            shakeDuration = duration;
            shakeTimeLeft = duration;
        }
    }
}
