using UnityEngine;

namespace Nocturne.Core
{
    /// <summary>
    /// Kinematic wall slide: our bodies are kinematic (no physics push), so plain
    /// MovePosition would teleport through World/Gate colliders. We sweep with
    /// Rigidbody2D.Cast first and fall back to per-axis moves (corner slide).
    /// Queued via MovePosition: for continuous per-FixedUpdate movers only.
    /// One-shot shoves must use <see cref="TryKnockback"/> instead — a queued
    /// MovePosition issued outside FixedUpdate is overwritten by the next mover
    /// MovePosition before the physics step, silently swallowing the shove.
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

        /// <summary>
        /// One-shot shove (hit knockback): same World/Gate sweep as
        /// <see cref="TryMove"/>, but travels as far as possible (shortened to
        /// contact instead of all-or-nothing) and applies instantly via
        /// <c>rb.position</c> so a later MovePosition in the same step can't
        /// overwrite it. Safe to call from input callbacks.
        /// </summary>
        public static void TryKnockback(Rigidbody2D rb, Vector2 delta, float skin = 0.05f)
        {
            if (rb == null || delta.sqrMagnitude < 1e-8f) return;

            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask((1 << Layers.World) | (1 << Layers.Gate));

            if (TryKnockbackAxis(rb, delta, filter, skin)) return;
            if (delta.x != 0f && TryKnockbackAxis(rb, new Vector2(delta.x, 0f), filter, skin)) return;
            if (delta.y != 0f) TryKnockbackAxis(rb, new Vector2(0f, delta.y), filter, skin);
        }

        private static bool TryKnockbackAxis(Rigidbody2D rb, Vector2 delta, ContactFilter2D filter, float skin)
        {
            var distance = delta.magnitude;
            if (distance < 1e-8f) return false;
            var dir = delta / distance;
            var allowed = distance;
            var count = rb.Cast(dir, filter, Buffer, distance);
            if (count > 0)
            {
                var nearest = distance;
                for (var i = 0; i < count; i++)
                    nearest = Mathf.Min(nearest, Buffer[i].distance);
                allowed = Mathf.Max(0f, nearest - Mathf.Max(0f, skin));
            }
            if (allowed < 1e-4f) return false;
            rb.position = rb.position + dir * allowed;
            return true;
        }
    }
}
