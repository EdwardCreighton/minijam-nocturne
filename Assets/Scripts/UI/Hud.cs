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
            var run = GameManager.Instance != null ? GameManager.Instance.Run : null;
            if (run == null) return;
            if (unspentText != null) unspentText.text = $"Points: {run.Unspent}";
            if (spentText != null) spentText.text = $"Spent: {run.SpentTotal}";
            if (deathsText != null) deathsText.text = $"Deaths: {run.Deaths}";
        }
    }
}
