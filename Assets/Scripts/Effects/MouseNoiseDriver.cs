using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Nocturne.Effects
{
    /// <summary>
    /// Drives the Noise module strength of a list of particle systems from a
    /// script-defined rest value toward a target value, based on mouse speed.
    /// At rest all systems sit at <see cref="baseStrength"/>; at full mouse
    /// speed all reach <see cref="targetStrength"/>.
    /// </summary>
    public sealed class MouseNoiseDriver : MonoBehaviour
    {
        [Tooltip("Particle systems whose Noise strength follows the mouse speed.")]
        public List<ParticleSystem> particleSystems = new();

        [Tooltip("Noise strength multiplier applied at rest (zero mouse speed).")]
        public float baseStrength = 1f;

        [Tooltip("Noise strength multiplier applied at full mouse speed.")]
        public float targetStrength = 3f;

        [Tooltip("Mouse speed in pixels/second that reaches the target strength.")]
        [Min(1f)] public float fullSpeed = 1500f;

        [Tooltip("Smoothing rate (1/s) for the measured mouse speed.")]
        [Min(0.1f)] public float smoothRate = 8f;

        private Vector2 prevMouse;
        private bool hasPrev;
        private float smoothSpeed;

        private void OnValidate()
        {
            if (fullSpeed < 1f) fullSpeed = 1f;
            if (smoothRate < 0.1f) smoothRate = 0.1f;
        }

        private void Update()
        {
            var mouse = ReadMousePosition();
            var udt = Time.unscaledDeltaTime;
            var instant = 0f;
            if (hasPrev && udt > 0f)
                instant = (mouse - prevMouse).magnitude / udt;
            prevMouse = mouse;
            hasPrev = true;

            smoothSpeed = Mathf.Lerp(smoothSpeed, instant,
                1f - Mathf.Exp(-smoothRate * Mathf.Max(0f, udt)));
            var t = Mathf.Clamp01(smoothSpeed / Mathf.Max(1f, fullSpeed));

            for (var i = particleSystems.Count - 1; i >= 0; i--)
            {
                var ps = particleSystems[i];
                if (ps == null)
                {
                    particleSystems.RemoveAt(i);
                    continue;
                }
                var noise = ps.noise;
                noise.strengthMultiplier = Mathf.Lerp(baseStrength, targetStrength, t);
            }
        }

        private static Vector2 ReadMousePosition()
        {
            var mouse = Mouse.current;
            return mouse != null ? mouse.position.ReadValue() : (Vector2)Input.mousePosition;
        }
    }
}
