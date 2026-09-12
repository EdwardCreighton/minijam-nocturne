using Nocturne.Config;
using Nocturne.Core;
using NUnit.Framework;
using UnityEngine;

namespace Nocturne.Tests.EditMode
{
    /// <summary>TZ §12.1: difficulty grows with Spent and never falls; capped.</summary>
    public sealed class DifficultyScalerTests
    {
        private BalanceConfig cfg;

        [SetUp]
        public void SetUp()
        {
            cfg = ScriptableObject.CreateInstance<BalanceConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Level_StepsEveryN_CappedAtMax(
            [Values(0, 50, 99)] int below,
            [Values(100, 150, 199)] int second,
            [Values(400, 10000)] int top)
        {
            Assert.AreEqual(1, DifficultyScaler.GetLevel(below, cfg));
            Assert.AreEqual(2, DifficultyScaler.GetLevel(second, cfg));
            Assert.AreEqual(5, DifficultyScaler.GetLevel(top, cfg));
        }

        [Test]
        public void Multipliers_GrowMonotonically()
        {
            var hp1 = DifficultyScaler.GetHpMult(1, cfg);
            var hp5 = DifficultyScaler.GetHpMult(5, cfg);
            var dmg1 = DifficultyScaler.GetDmgMult(1, cfg);
            var dmg5 = DifficultyScaler.GetDmgMult(5, cfg);
            Assert.AreEqual(1f, hp1);
            Assert.AreEqual(1f, dmg1);
            Assert.Greater(hp5, hp1);
            Assert.Greater(dmg5, dmg1);
            Assert.AreEqual(2f, hp5, 0.001f);
            Assert.AreEqual(1.6f, dmg5, 0.001f);
        }
    }
}
