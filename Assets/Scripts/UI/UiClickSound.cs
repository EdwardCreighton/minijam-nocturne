using Nocturne.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Nocturne.UI
{
    /// <summary>
    /// Короткий звук при нажатии на кнопку UI.
    /// Повесьте на любой Button: сам клип настраивается один раз
    /// в <see cref="AudioManager"/> (поле Click Sound).
    /// Не вешать на кнопки, чьи обработчики уже зовут AudioManager.Click/ClickThenLoad
    /// (кнопки меню и экранов уровня), — будет двойной звук.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class UiClickSound : MonoBehaviour
    {
        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            if (button != null)
                button.onClick.AddListener(AudioManager.Click);
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(AudioManager.Click);
        }
    }
}
