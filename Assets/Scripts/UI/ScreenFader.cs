using UnityEngine;
using UnityEngine.UI;

namespace Nocturne.UI
{
    /// <summary>
    /// Чёрная шторка поверх меню: плавное затемнение при переходе в уровень.
    /// Только картинка — загрузкой сцены управляет MainMenuController.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public sealed class ScreenFader : MonoBehaviour
    {
        private Image image;

        private void Awake()
        {
            image = GetComponent<Image>();
            SetAlpha(0f);
            if (image != null)
                image.raycastTarget = false;
        }

        /// <summary>
        /// Затемнение 0→1 за seconds (unscaled-время). Сразу включает raycastTarget,
        /// чтобы заблокировать повторные нажатия на время перехода.
        /// </summary>
        public System.Collections.IEnumerator FadeOut(float seconds)
        {
            if (image != null)
                image.raycastTarget = true;
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                SetAlpha(Mathf.Clamp01(t / Mathf.Max(seconds, 0.0001f)));
                yield return null;
            }
            SetAlpha(1f);
        }

        private void SetAlpha(float a)
        {
            if (image == null) return;
            var c = image.color;
            c.a = a;
            image.color = c;
        }
    }
}
