using Nocturne.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nocturne.UI
{
    /// <summary>
    /// Level screens (TZ §10): Briefing / Death / Win / Pause. Observes
    /// GameManager.StateChanged; buttons call back into GameManager/SceneLoader.
    /// Panels are built by setup; this only toggles + fills texts.
    /// </summary>
    public sealed class Screens : MonoBehaviour
    {
        public GameObject briefingPanel;
        public TextMeshProUGUI briefingText;
        public Button briefingStartButton;

        public GameObject deathPanel;
        public TextMeshProUGUI deathText;

        public GameObject winPanel;
        public TextMeshProUGUI winText;
        public Button winMenuButton;
        public Button winNewGameButton;

        public GameObject pausePanel;
        public Button pauseResumeButton;
        public Button pauseMenuButton;

        private void Start()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            if (briefingStartButton != null)
                briefingStartButton.onClick.AddListener(() => gm.DismissBriefing());
            if (pauseResumeButton != null)
                pauseResumeButton.onClick.AddListener(() => gm.TogglePause());
            if (pauseMenuButton != null)
                pauseMenuButton.onClick.AddListener(SceneLoader.LoadMainMenu);
            if (winMenuButton != null)
                winMenuButton.onClick.AddListener(SceneLoader.LoadMainMenu);
            if (winNewGameButton != null)
                winNewGameButton.onClick.AddListener(SceneLoader.LoadGameLevel);

            gm.StateChanged += OnStateChanged;
            OnStateChanged(gm.State);
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.StateChanged -= OnStateChanged;
        }

        private void OnStateChanged(GameState state)
        {
            var gm = GameManager.Instance;
            SetActive(briefingPanel, state == GameState.Briefing);
            SetActive(deathPanel, state == GameState.Dying);
            SetActive(winPanel, state == GameState.Win);
            SetActive(pausePanel, state == GameState.Paused);

            if (state == GameState.Win && gm != null && winText != null)
            {
                var run = gm.Run;
                var time = Mathf.FloorToInt(run.RunTime);
                winText.text = $"Финиш: {run.FinishId}\nПотрачено: {run.SpentTotal}\nСмертей: {run.Deaths}\nВремя: {time / 60:00}:{time % 60:00}";
            }

            if (state == GameState.Dying && deathText != null)
                deathText.text = "Ты погиб. Сон начинается заново...";
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null && go.activeSelf != active)
                go.SetActive(active);
        }
    }
}
