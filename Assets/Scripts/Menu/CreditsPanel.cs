using UnityEngine;

namespace Nocturne.Menu
{
    /// <summary>
    /// Credits panel: team, assets/music, jam version. Plain show/hide.
    /// </summary>
    public sealed class CreditsPanel : MonoBehaviour
    {
        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);
    }
}
