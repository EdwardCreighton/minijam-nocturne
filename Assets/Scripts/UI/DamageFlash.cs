using UnityEngine;
using UnityEngine.UI;

namespace Nocturne.UI
{
    /// <summary>
    /// Fullscreen red flicker when the player takes damage.
    /// Builds its own overlay Image under the parent canvas at runtime
    /// (first sibling, so panels and buttons stay above it) — no scene setup.
    /// Self-attached by Hud, so it works with the existing HudCanvas as is.
    /// </summary>
    public sealed class DamageFlash : MonoBehaviour
    {
        private const float PeakAlpha = 0.45f;
        private const float FadeTime = 0.45f;

        private Image overlay;
        private Player.PlayerHealth playerHealth;
        private float alpha;

        private void Awake()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
                canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                enabled = false;
                return;
            }

            var go = new GameObject("DamageFlash",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(canvas.transform, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.transform.SetAsFirstSibling();

            overlay = go.GetComponent<Image>();
            overlay.color = new Color(1f, 0.1f, 0.1f, 0f);
            overlay.raycastTarget = false;
        }

        private void Start()
        {
            var playerGo = GameObject.FindWithTag("Player");
            if (playerGo != null)
            {
                playerHealth = playerGo.GetComponent<Player.PlayerHealth>();
                if (playerHealth != null)
                    playerHealth.Damaged += OnDamaged;
            }
        }

        private void OnDestroy()
        {
            if (playerHealth != null)
                playerHealth.Damaged -= OnDamaged;
        }

        private void OnDamaged()
        {
            alpha = PeakAlpha;
            Apply();
        }

        private void Update()
        {
            if (overlay == null || alpha <= 0f) return;
            // Unscaled so the flash always completes even if timeScale changes.
            alpha = Mathf.Max(0f, alpha - PeakAlpha * Time.unscaledDeltaTime / FadeTime);
            Apply();
        }

        private void Apply()
        {
            var c = overlay.color;
            c.a = alpha;
            overlay.color = c;
        }
    }
}
