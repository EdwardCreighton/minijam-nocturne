using Nocturne.Config;
using UnityEngine;

namespace Nocturne.Core
{
    /// <summary>
    /// Difficulty = f(SpentTotal) (TZ §8.2). Pure static functions: no state, no
    /// per-enemy formulas. Multipliers are injected at spawn time only.
    /// </summary>
    public static class DifficultyScaler
    {
        public static int GetLevel(int spentTotal, BalanceConfig cfg)
        {
            var n = cfg != null ? cfg.difficultyN : 100;
            var max = cfg != null ? cfg.maxLevel : 5;
            return Mathf.Clamp(1 + Mathf.FloorToInt((float)spentTotal / Mathf.Max(1, n)), 1, Mathf.Max(1, max));
        }

        public static float GetHpMult(int level, BalanceConfig cfg)
        {
            var k = cfg != null ? cfg.difficultyK : 0.25f;
            return 1f + Mathf.Max(0f, k) * (level - 1);
        }

        public static float GetDmgMult(int level, BalanceConfig cfg)
        {
            var m = cfg != null ? cfg.difficultyM : 0.15f;
            return 1f + Mathf.Max(0f, m) * (level - 1);
        }
    }
}
