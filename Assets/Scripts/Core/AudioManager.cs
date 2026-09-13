using UnityEngine;

namespace Nocturne.Core
{
    /// <summary>
    /// Central sound API: UI click is real (Click Sound clip, assigned per scene),
    /// gameplay sounds are still stubs writing [Audio] id to the console (P8).
    /// No native plugins, first audible sound only after the first user gesture (TZ §2.1).
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Tooltip("Короткий звук клика по кнопкам UI. Назначьте AudioClip в инспекторе.")]
        [SerializeField] private AudioClip clickSound;

        [Header("Menu music")]
        [Tooltip("Фоновый трек. Назначается только в сцене меню.")]
        [SerializeField] private AudioClip musicTrack;
        [Tooltip("Громкость фоновой музыки.")]
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.7f;
        [Tooltip("Запустить musicTrack в loop при старте сцены. Включено только в MainMenu.")]
        [SerializeField] private bool playMusicOnStart;

        private AudioSource source;
        private AudioSource musicSource;
        private bool transitionPending;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            source = GetComponent<AudioSource>();
            if (source == null)
                source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            // UI-клики слышны при AudioListener.pause (пауза/брифинг/победа).
            // Заодно не глушатся будущие джинглы победы/смерти: GameManager ставит
            // pause сразу после PlayWin/PlayDeath. Побочный эффект: будущие боевые
            // клипы (swing/hit) тоже будут игнорировать паузу — осознанно, в паузе боя нет.
            source.ignoreListenerPause = true;
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Start()
        {
            if (playMusicOnStart)
                PlayMusic();
        }

        /// <summary>Запускает фоновый трек в loop (если назначен).</summary>
        public void PlayMusic()
        {
            if (musicTrack == null) return;
            if (musicSource == null)
            {
                // Отдельный источник: фейд музыки не должен глушить клики PlayOneShot.
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
            }
            musicSource.clip = musicTrack;
            musicSource.volume = musicVolume;
            if (!musicSource.isPlaying)
                musicSource.Play();
        }

        /// <summary>
        /// Плавно гасит музыку до нуля за fadeSeconds и останавливает.
        /// Ждёт в unscaled-времени — переходы живут при timeScale = 0.
        /// </summary>
        public void StopMusic(float fadeSeconds = 1f)
        {
            if (musicSource == null || !musicSource.isPlaying) return;
            StartCoroutine(FadeMusicOut(Mathf.Max(0f, fadeSeconds)));
        }

        private System.Collections.IEnumerator FadeMusicOut(float fadeSeconds)
        {
            float startVolume = musicSource.volume;
            if (fadeSeconds <= 0f)
            {
                musicSource.Stop();
                musicSource.volume = musicVolume;
                yield break;
            }
            float t = 0f;
            while (t < fadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                if (musicSource != null)
                    musicSource.volume = Mathf.Lerp(startVolume, 0f, Mathf.Clamp01(t / fadeSeconds));
                yield return null;
            }
            if (musicSource != null)
            {
                musicSource.Stop();
                musicSource.volume = musicVolume;
            }
        }

        public void PlaySwing() => PlayCombatConfigured("swing", isPlayer: true);
        public void PlayEnemyAttack() => PlayCombatConfigured("enemy_attack", isPlayer: false);
        public void PlayFootsteps() => PlayFootstepsConfigured();
        public void PlayHit() => PlayStub("hit");
        public void PlayGateOpen() => PlayStub("gate_open");
        public void PlayDeath() => PlayStub("death");
        public void PlayWin() => PlayStub("win");
        public void PlayClick()
        {
            if (clickSound != null && source != null)
            {
                source.PlayOneShot(clickSound);
                return;
            }
            PlayStub("click");
        }

        /// <summary>
        /// Null-safe точки входа. Единственное место, откуда зовут звук, —
        /// вместо копий `if (Instance != null)` по контроллерам.
        /// </summary>
        public static void Click()
        {
            if (Instance != null)
                Instance.PlayClick();
        }

        public static void Swing()
        {
            if (Instance != null)
                Instance.PlaySwing();
        }

        public static void EnemyAttack()
        {
            if (Instance != null)
                Instance.PlayEnemyAttack();
        }

        public static void Footsteps()
        {
            if (Instance != null)
                Instance.PlayFootsteps();
        }

        public static void Hit()
        {
            if (Instance != null)
                Instance.PlayHit();
        }

        public static void GateOpen()
        {
            if (Instance != null)
                Instance.PlayGateOpen();
        }

        public static void Death()
        {
            if (Instance != null)
                Instance.PlayDeath();
        }

        public static void Win()
        {
            if (Instance != null)
                Instance.PlayWin();
        }

        /// <summary>
        /// Клик + отложенная смена сцены: синхронный LoadScene уничтожает
        /// AudioSource в конце кадра и обрезает звук, поэтому ждём долю секунды.
        /// Без явной задержки ждём длину клипа + 0.05с (минимум 0.05с).
        /// Ждём в реальном времени — переходы происходят при timeScale = 0
        /// (пауза/победа). Повторные вызовы до завершения перехода игнорируются.
        /// Если AudioManager отсутствует — грузим сразу.
        /// </summary>
        public static void ClickThenLoad(System.Action loadAction, float delaySeconds = -1f)
        {
            if (Instance != null)
            {
                float d = delaySeconds < 0f && Instance.clickSound != null
                    ? Mathf.Max(0.05f, Instance.clickSound.length + 0.05f)
                    : Mathf.Max(0f, delaySeconds);
                Instance.PlayClickThenLoad(loadAction, d);
            }
            else
            {
                loadAction?.Invoke();
            }
        }

        private void PlayClickThenLoad(System.Action loadAction, float delaySeconds)
        {
            PlayClick();
            if (loadAction == null || transitionPending) return;
            transitionPending = true;
            StartCoroutine(LoadAfterDelay(loadAction, delaySeconds));
        }

        private static System.Collections.IEnumerator LoadAfterDelay(System.Action loadAction, float delay)
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, delay));
            try
            {
                loadAction.Invoke();
            }
            finally
            {
                // При успешной загрузке объект умрёт вместе с корутиной; сброс нужен
                // на случай, если сцена не сменилась (исключение), — иначе кнопки
                // переходов навсегда перестанут грузить.
                if (Instance != null)
                    Instance.transitionPending = false;
            }
        }

        /// <summary>
        /// Combat one-shot from the main config (BalanceConfig SFX section).
        /// Clips are assigned on the config asset; without a clip (or source)
        /// it falls back to the [Audio] stub so call sites never branch.
        /// </summary>
        private void PlayCombatConfigured(string stubId, bool isPlayer)
        {
            var cfg = GameManager.Instance != null ? GameManager.Instance.config : null;
            var clip = cfg != null
                ? (isPlayer ? cfg.playerAttackSound : cfg.enemyAttackSound)
                : null;
            var volume = cfg != null ? cfg.combatVolume : 1f;
            if (clip != null && source != null)
            {
                source.PlayOneShot(clip, volume);
                return;
            }
            PlayStub(stubId);
        }

        /// <summary>
        /// Footstep one-shot from the main config, played at footstepsPitch.
        /// PlayOneShot has no pitch parameter, so the source pitch is set for
        /// the call and restored right after (the one-shot captures it).
        /// </summary>
        private int footstepIndex;

        private void PlayFootstepsConfigured()
        {
            var cfg = GameManager.Instance != null ? GameManager.Instance.config : null;
            var clip = PickNextFootstep(cfg != null ? cfg.footstepsSounds : null);
            if (clip == null || source == null)
            {
                PlayStub("footsteps");
                return;
            }
            source.pitch = cfg != null ? Mathf.Max(0.1f, cfg.footstepsPitch) : 1f;
            source.PlayOneShot(clip, cfg != null ? cfg.combatVolume : 1f);
            source.pitch = 1f;
        }

        /// <summary>
        /// Round-robin pick: plays clips in array order, 1..N, 1..N.
        /// Empty slots are skipped; null when nothing playable is assigned.
        /// </summary>
        private AudioClip PickNextFootstep(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0) return null;
            for (var i = 0; i < clips.Length; i++)
            {
                var clip = clips[footstepIndex % clips.Length];
                footstepIndex++;
                if (clip != null) return clip;
            }
            return null;
        }

        private void PlayStub(string id)
        {
            // TODO: assign real clips; keep Vorbis + compressed (TZ §2.1).
            Debug.Log($"[Audio] {id}");
        }
    }
}
