using System.Collections.Generic;
using Nocturne.Config;
using Nocturne.World;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nocturne.Tests.EditMode
{
    /// <summary>TZ §12.1: config defaults + gate id uniqueness in the level.</summary>
    public sealed class BalanceAndGateTests
    {
        [Test]
        public void BalanceConfig_HasTzDefaults()
        {
            var cfg = AssetDatabase.LoadAssetAtPath<BalanceConfig>("Assets/Settings/BalanceConfig.asset");
            Assert.IsNotNull(cfg);
            Assert.AreEqual(100, cfg.difficultyN);
            Assert.AreEqual(0.25f, cfg.difficultyK);
            Assert.AreEqual(0.15f, cfg.difficultyM);
            Assert.AreEqual(5, cfg.maxLevel);
            Assert.AreEqual(0.6f, cfg.holdTime);
            Assert.Greater(cfg.damageCooldown, 0f);
        }

        [Test]
        public void Gates_HaveUniqueIds_AndPositiveCosts()
        {
            var previous = SceneManager.GetActiveScene().path;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            try
            {
                var gates = Object.FindObjectsByType<Gate>(FindObjectsSortMode.None);
                Assert.GreaterOrEqual(gates.Length, 2, "level needs 2+ gates");
                var ids = new HashSet<string>();
                foreach (var gate in gates)
                {
                    Assert.IsFalse(string.IsNullOrEmpty(gate.id), gate.name);
                    Assert.Greater(gate.cost, 0, gate.name);
                    Assert.IsTrue(ids.Add(gate.id), $"duplicate gate id: {gate.id}");
                }
            }
            finally
            {
                if (!string.IsNullOrEmpty(previous))
                    EditorSceneManager.OpenScene(previous);
            }
        }
    }
}
