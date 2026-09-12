using Nocturne.Core;
using Nocturne.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Nocturne.Menu
{
    /// <summary>
    /// Main menu navigation (TZ §4.0): New Game -> GameLevel (fresh RunState is
    /// created by the scene itself, so no explicit reset is needed), Credits panel
    /// toggle. No game logic here.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        public Button newGameButton;
        public Button creditsButton;
        public Button backButton;
        public CreditsPanel creditsPanel;
        [Tooltip("Title + menu buttons. Hidden while credits are shown.")]
        public GameObject menuRoot;
        [Tooltip("Чёрная шторка затемнения при переходе в уровень.")]
        public ScreenFader fader;
        [Tooltip("Длительность затемнения и затухания музыки при старте игры.")]
        public float transitionFade = 1f;

        private bool transitionPending;

        private void Start()
        {
            if (newGameButton != null)
                newGameButton.onClick.AddListener(OnNewGame);
            if (creditsButton != null)
                creditsButton.onClick.AddListener(OnCredits);
            if (backButton != null)
                backButton.onClick.AddListener(OnBack);
            if (creditsPanel != null)
                creditsPanel.Hide();
            if (menuRoot != null)
                menuRoot.SetActive(true);
        }

        private void OnNewGame()
        {
            if (transitionPending) return;
            transitionPending = true;
            StartCoroutine(TransitionToLevel());
        }

        /// <summary>
        /// Клик → параллельные затемнение и затухание музыки → только потом сцена уровня.
        /// </summary>
        private System.Collections.IEnumerator TransitionToLevel()
        {
            AudioManager.Click();
            float fade = Mathf.Max(0f, transitionFade);
            var audio = AudioManager.Instance;
            if (audio != null) audio.StopMusic(fade);
            if (fader == null)
                Debug.LogWarning("[Menu] ScreenFader is not assigned on MainMenuController — transition runs blind.");
            if (fader != null)
                yield return fader.FadeOut(fade);
            else
                yield return new WaitForSecondsRealtime(fade);
            SceneLoader.LoadGameLevel();
        }

        private void OnCredits()
        {
            AudioManager.Click();
            if (menuRoot != null) menuRoot.SetActive(false);
            if (creditsPanel != null) creditsPanel.Show();
        }

        private void OnBack()
        {
            AudioManager.Click();
            if (creditsPanel != null) creditsPanel.Hide();
            if (menuRoot != null) menuRoot.SetActive(true);
        }
    }
}
