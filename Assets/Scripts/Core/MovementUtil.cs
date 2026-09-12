using UnityEngine;

namespace Nocturne.Core
{
    /// <summary>
    /// Kinematic wall slide: our bodies are kinematic (no physics push), so plain
    /// MovePosition would teleport through World/Gate colliders. We sweep with
    /// Rigidbody2D.Cast first and fall back to per-axis moves (corner slide).
    /// </summary>
    public static class MovementUtil
    {
        private static readonly RaycastHit2D[] Buffer = new RaycastHit2D[8];

        public static void TryMove(Rigidbody2D rb, Vector2 delta)
        {
            if (rb == null || delta.sqrMagnitude < 1e-8f) return;

            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask((1 << Layers.World) | (1 << Layers.Gate));

            if (TryAxis(rb, delta, filter)) return;
            if (delta.x != 0f && TryAxis(rb, new Vector2(delta.x, 0f), filter)) return;
            if (delta.y != 0f) TryAxis(rb, new Vector2(0f, delta.y), filter);
        }

        private static bool TryAxis(Rigidbody2D rb, Vector2 delta, ContactFilter2D filter)
        {
            var distance = delta.magnitude;
            if (distance < 1e-8f) return false;
            // Cast excludes the body's own colliders; any hit is a real obstacle.
            if (rb.Cast(delta / distance, filter, Buffer, distance) > 0) return false;
            rb.MovePosition(rb.position + delta);
            return true;
        }
    }
}
