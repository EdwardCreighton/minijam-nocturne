using System.Collections;
using Nocturne.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Nocturne.Tests.PlayMode
{
    /// <summary>TZ §12.1: Pause freezes time + shows the pause screen; Finish ends the run.</summary>
    public sealed class FlowTests
    {
        private const string LevelScene = "SampleScene";

        [UnityTest]
        public IEnumerator Pause_StopsTime_AndShowsScreen()
        {
            yield return LoadLevel();
            var gm = GameManager.Instance;

            gm.TogglePause();
            yield return null;
            Assert.AreEqual(0f, Time.timeScale);
            Assert.AreEqual(GameState.Paused, gm.State);
            var pausePanel = GameObject.Find("PausePanel");
            Assert.IsNotNull(pausePanel);
            Assert.IsTrue(pausePanel.activeSelf);

            gm.TogglePause();
            yield return null;
            Assert.AreEqual(1f, Time.timeScale);
            Assert.AreEqual(GameState.Playing, gm.State);
            Assert.IsFalse(pausePanel.activeSelf);
        }

        [UnityTest]
        public IEnumerator Finish_EndsRun_WithStats()
        {
            yield return LoadLevel();
            var gm = GameManager.Instance;

            var playerGo = GameObject.FindWithTag("Player");
            playerGo.transform.position = new Vector3(17f, 0f, 0f); // Finish_East trigger
            // Unscaled poll: OnFinishReached freezes timeScale, so scaled waits would hang.
            var elapsed = 0f;
            while (gm.State != GameState.Win && elapsed < 3f)
            {
                yield return new WaitForSecondsRealtime(0.1f);
                elapsed += 0.1f;
            }

            Assert.AreEqual(GameState.Win, gm.State);
            Assert.AreEqual("finish_east", gm.Run.FinishId);
            var winPanel = GameObject.Find("WinPanel");
            Assert.IsNotNull(winPanel);
            Assert.IsTrue(winPanel.activeSelf);
        }

        private IEnumerator LoadLevel()
        {
            SceneManager.LoadScene(LevelScene);
            yield return null;
            yield return null;
            GameManager.Instance.DismissBriefing();
            yield return null;
        }
    }
}
