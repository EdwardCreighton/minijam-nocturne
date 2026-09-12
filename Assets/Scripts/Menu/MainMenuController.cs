using Nocturne.Core;
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

        private void OnNewGame() => AudioManager.ClickThenLoad(SceneLoader.LoadGameLevel);

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
