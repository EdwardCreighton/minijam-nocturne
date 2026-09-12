using UnityEngine.SceneManagement;

namespace Nocturne.Core
{
    /// <summary>
    /// Scene transitions Menu &lt;-&gt; Level only (TZ §4). No LoadScene inside GameLevel.
    /// </summary>
    public static class SceneLoader
    {
        public const string MainMenu = "MainMenu";
        // TODO: rename SampleScene to GameLevel via Editor (keeps GUID), then update this constant.
        public const string GameLevel = "SampleScene";

        public static void LoadMainMenu() => SceneManager.LoadScene(MainMenu);
        public static void LoadGameLevel() => SceneManager.LoadScene(GameLevel);
    }
}
