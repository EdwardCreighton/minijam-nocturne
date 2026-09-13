using Nocturne.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nocturne.UI
{
    /// <summary>
    /// Level HUD numbers (TZ §10): HP / Unspent / Spent(+level) / Deaths.
    /// hpText is wired by setup; HP updates come from PlayerHealth.HPChanged.
    /// </summary>
    public sealed class Hud : MonoBehaviour
    {
        public TextMeshProUGUI hpText;
        public TextMeshProUGUI unspentText;
        public TextMeshProUGUI spentText;
        public TextMeshProUGUI deathsText;

        private Player.PlayerHealth playerHealth;

        // Subscription lives in Start (not Awake/OnEnable): all scene Awakes —
        // including GameManager's, which creates the RunState — run first.
        private void Start()
        {
            // Damage flash needs no scene setup: it builds its own overlay.
            if (GetComponent<DamageFlash>() == null)
                gameObject.AddComponent<DamageFlash>();
            Refresh();
            if (GameManager.Instance != null)
                GameManager.Instance.Run.Changed += Refresh;

            var playerGo = GameObject.FindWithTag("Player");
            if (playerGo != null)
            {
                playerHealth = playerGo.GetComponent<Player.PlayerHealth>();
                if (playerHealth != null)
                    playerHealth.HPChanged += RefreshHP;
            }

            RefreshHP();
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.Run.Changed -= Refresh;
            if (playerHealth != null)
                playerHealth.HPChanged -= RefreshHP;
        }

        private void Refresh()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            var run = gm.Run;
            if (unspentText != null) unspentText.text = $"Points: {run.Unspent}";
            if (spentText != null)
            {
                var level = DifficultyScaler.GetLevel(run.SpentTotal, gm.config);
                spentText.text = $"Spent: {run.SpentTotal} (lvl {level})";
            }

            if (deathsText != null) deathsText.text = $"Deaths: {run.Deaths}";
        }

        private void RefreshHP()
        {
            if (hpText == null) return;
            hpText.text = playerHealth != null
                ? $"HP: {playerHealth.CurrentHP}/{playerHealth.MaxHP}"
                : "HP: —";
        }
    }
}
