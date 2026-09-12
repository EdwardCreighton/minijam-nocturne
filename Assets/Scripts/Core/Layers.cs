using UnityEngine;

namespace Nocturne.Core
{
    /// <summary>
    /// Physics layer indices shared by code and scenes (TZ §5.1).
    /// Names are assigned to TagManager by the P1 setup; collisions are
    /// enforced at runtime in GameManager so behaviour never depends on
    /// checked-in Physics2D matrix state.
    /// </summary>
    public static class Layers
    {
        public const int Player = 6;
        public const int Enemy = 7;
        public const int World = 8;
        public const int Gate = 9;

        public static void ApplyCollisionMatrix()
        {
            // Start from "everything collides", then carve out exceptions.
            for (var i = 0; i < 32; i++)
            for (var j = 0; j < 32; j++)
                Physics2D.IgnoreLayerCollision(i, j, false);

            // Enemies do not collide with each other (separation is steering-based).
            Physics2D.IgnoreLayerCollision(Enemy, Enemy, true);
            // Closed gates block both player and enemies (default: collide).
        }
    }
}
