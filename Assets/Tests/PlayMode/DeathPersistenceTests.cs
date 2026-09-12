using System.Collections;
using Nocturne.Core;
using Nocturne.Player;
using Nocturne.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Nocturne.Tests.PlayMode
{
    /// <summary>TZ §12.1: gate hold → death → gate open, Spent kept, Unspent == 0, back at Start.</summary>
    public sealed class DeathPersistenceTests
    {
        private const string LevelScene = "SampleScene";

        [UnityTest]
        public IEnumerator Death_KeepsGatesAndSpent_WipesUnspent_RespawnsAtStart()
        {
            SceneManager.LoadScene(LevelScene);
            yield return null;
            yield return null;

            var gm = GameManager.Instance;
            Assert.IsNotNull(gm);
            gm.DismissBriefing();
            yield return null;

            gm.Run.AddKill(200);
            var gate = GameObject.Find("Gate_East").GetComponent<Gate>();
            Assert.IsTrue(gate.TryOpen(gm.Run), "precondition: gate opens");
            Assert.AreEqual(30, gm.Run.SpentTotal);

            var playerGo = GameObject.FindWithTag("Player");
            var start = Object.FindFirstObjectByType<StartPoint>().transform.position;
            // Move away so the respawn position assertion is meaningful.
            playerGo.transform.position = start + new Vector3(5f, 0f, 0f);

            playerGo.GetComponent<PlayerHealth>().TakeDamage(99999, playerGo.transform.position);
            Assert.AreEqual(GameState.Dying, gm.State);
            yield return new WaitForSeconds(2f);

            Assert.AreEqual(GameState.Playing, gm.State);
            Assert.IsTrue(gate.isOpen, "gate stays open after death");
            Assert.AreEqual(30, gm.Run.SpentTotal, "spent never decreases");
            Assert.AreEqual(0, gm.Run.Unspent);
            Assert.AreEqual(1, gm.Run.Deaths);
            Assert.Less(Vector2.Distance(playerGo.transform.position, start), 0.05f, "respawn at Start");
        }
    }
}
