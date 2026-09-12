using UnityEngine;

namespace Nocturne.Core
{
    /// <summary>
    /// Sound stub (P8): central API + silent AudioSource so call sites exist and
    /// can be verified. Real clips replace the Debug.Log calls; no native plugins,
    /// first audible sound only after the first user gesture (TZ §2.1).
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private AudioSource source;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            source = GetComponent<AudioSource>();
            if (source == null)
                source = gameObject.AddComponent<AudioSource>();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void PlaySwing() => PlayStub("swing");
        public void PlayHit() => PlayStub("hit");
        public void PlayGateOpen() => PlayStub("gate_open");
        public void PlayDeath() => PlayStub("death");
        public void PlayWin() => PlayStub("win");
        public void PlayClick() => PlayStub("click");

        private void PlayStub(string id)
        {
            // TODO: assign real clips; keep Vorbis + compressed (TZ §2.1).
            Debug.Log($"[Audio] {id}");
        }
    }
}
