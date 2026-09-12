using UnityEngine;

namespace Nocturne.Combat
{
    /// <summary>
    /// Anything that can take hits (player, enemies). PlayerCombat hits this
    /// on the Enemy layer; enemies hit the player through the same contract (P3).
    /// </summary>
    public interface IDamageable
    {
        bool IsAlive { get; }
        void TakeDamage(int amount, Vector2 sourcePosition);
    }
}
