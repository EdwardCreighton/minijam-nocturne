using Nocturne.Core;
using Nocturne.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nocturne.UI
{
    /// <summary>
    /// Contextual gate prompt (TZ §7.1, §10): shows while the player stands near a
    /// closed gate — «держи E — открыть (cost)» or the missing-funds variant —
    /// plus a hold progress ring/bar. Screen-space, follows nothing (single target).
    /// </summary>
    public sealed class GatePrompt : MonoBehaviour
    {
        public TextMeshProUGUI promptText;
        public Image progressImage;

        private Transform player;
        private Player.PlayerInteractor interactor;

        private void Start()
        {
            var playerGo = GameObject.FindWithTag("Player");
            if (playerGo != null)
            {
                player = playerGo.transform;
                interactor = playerGo.GetComponent<Player.PlayerInteractor>();
            }

            SetVisible(false);
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || interactor == null || gm.State != GameState.Playing)
            {
                SetVisible(false);
                return;
            }

            var gate = interactor.CurrentGate;
            if (gate == null)
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);
            var missing = gate.cost - gm.Run.Unspent;
            if (promptText != null)
            {
                promptText.text = missing <= 0
                    ? $"Держи E — открыть ({gate.cost})"
                    : $"Держи E — открыть ({gate.cost}), не хватает {missing}";
            }

            if (progressImage != null)
                progressImage.fillAmount = Mathf.Clamp01(interactor.HoldProgress);
        }

        private void SetVisible(bool visible)
        {
            if (promptText != null) promptText.enabled = visible;
            if (progressImage != null) progressImage.enabled = visible;
        }
    }
}
