using Nocturne.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Nocturne.UI
{
    /// <summary>
    /// Level HUD numbers (TZ §10). P1: Unspent / Spent(+level stub) / Deaths.
    /// HP row and gate prompt land in P2/P4.
    /// </summary>
    public sealed class Hud : MonoBehaviour
    {
        public Text unspentText;
        public Text spentText;
        public Text deathsText;

        // Subscription lives in Start (not Awake/OnEnable): all scene Awakes —
        // including GameManager's, which creates the RunState — run first.
        private void Start()
        {
            Refresh();
            if (GameManager.Instance != null)
                GameManager.Instance.Run.Changed += Refresh;
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.Run.Changed -= Refresh;
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
    }
}
