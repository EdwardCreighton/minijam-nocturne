using UnityEngine;
using UnityEngine.Serialization;

namespace Nocturne.Core
{
    /// <summary>
    /// Central sound API: UI click is real (Click Sound clip, assigned per scene),
    /// gameplay sounds are still stubs writing [Audio] id to the console (P8).
    /// Level background music is a per-track-volume playlist (shuffle, no repeats
    /// until exhausted); menu uses the single looped musicTrack. Never both.
    /// No native plugins, first audible sound only after the first user gesture (TZ §2.1).
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        /// <summary>One entry of the level playlist: clip + its own volume.</summary>
        [System.Serializable]
        public sealed class LevelMusicTrack
        {
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 0.7f;
        }

        public static AudioManager Instance { get; private set; }

        [Tooltip("Короткий звук клика по кнопкам UI. Назначьте AudioClip в инспекторе.")]
        [SerializeField] private AudioClip clickSound;

        [Header("Menu music")]
        [Tooltip("Фоновый трек. Назначается только в сцене меню.")]
        [SerializeField] private AudioClip musicTrack;
        [Tooltip("Общая громкость музыки (меню и множитель для плейлиста уровня).")]
        [SerializeField, Range(0f, 1f)]
        [FormerlySerializedAs("musicVolume")]
        private float globalMusicVolume = 0.7f;
        [Tooltip("Запустить musicTrack в loop при старте сцены. Включено только в MainMenu.")]
        [SerializeField] private bool playMusicOnStart;

        [Header("Level playlist")]
        [Tooltip("Треки фона уровня. Первый — случайный (очередь перемешивается), дальше по очереди без повторов; в конце — новое перемешивание. Пусто в MainMenu.")]
        [SerializeField] private System.Collections.Generic.List<LevelMusicTrack> levelPlaylist = new();

        [SerializeField] private AudioClip playerSwing;
        [SerializeField] private AudioClip hit;
        [SerializeField] private AudioClip enemySwing;

        private AudioSource source;
        private AudioSource musicSource;
        private bool transitionPending;

        // --- Level playlist state (v2: single trigger, pause-aware, sharp cuts) ---
        private bool playlistActive;
        private System.Collections.Generic.List<int> playlistQueue = new();
        private int playlistPos;
        private float lastTrackStartUnscaled = -100f;
        private float currentEffectiveVolume;
        private const float TrackStartGrace = 0.5f;

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
            playlistActive = false;
            if (Instance == this)
                Instance = null;
        }

        private void OnValidate()
        {
            globalMusicVolume = Mathf.Clamp01(globalMusicVolume);
            if (levelPlaylist == null) return;
            foreach (var t in levelPlaylist)
            {
                if (t == null) continue;
                t.volume = Mathf.Clamp01(t.volume);
                if (t.clip == null)
                    Debug.LogWarning("[Audio] Level playlist has an entry without a clip — it will be skipped.", this);
            }
        }

        private void Update()
        {
            if (!playlistActive || musicSource == null) return;
            // Пауза с миром: пока AudioListener.pause музыка стоит, isPlaying
            // не падает, но проверку все равно пропускаем — иначе десинк.
            if (AudioListener.pause) return;
            if (Time.unscaledTime - lastTrackStartUnscaled < TrackStartGrace) return;
            if (!musicSource.isPlaying)
                NextTrack();
        }

        private void Start()
        {
            if (playMusicOnStart)
                PlayMusic();
        }

        /// <summary>Запускает фоновый трек в loop (если назначен). Только меню.</summary>
        public void PlayMusic()
        {
            if (musicTrack == null) return;
            playlistActive = false;
            EnsureMusicSource();
            musicSource.loop = true;
            musicSource.clip = musicTrack;
            currentEffectiveVolume = globalMusicVolume;
            musicSource.volume = currentEffectiveVolume;
            if (!musicSource.isPlaying)
                musicSource.Play();
        }

        /// <summary>
        /// Стартует плейлист уровня: перемешанная очередь (случайный первый),
        /// дальше по очереди без повторов, в конце — новое перемешивание.
        /// Единственный триггер — GameManager.DismissBriefing (первый жест, TZ §2.1).
        /// Повторный вызов во время игры — no-op. Один трек — loop его же.
        /// Пустой плейлист — тишина + варнинг.
        /// </summary>
        public void PlayLevelPlaylist()
        {
            if (playlistActive && musicSource != null && musicSource.isPlaying) return;
            playlistQueue = BuildQueueForTracks(levelPlaylist, UnityEngine.Random.Range(int.MinValue, int.MaxValue));
            if (playlistQueue.Count == 0)
            {
                Debug.LogWarning("[Audio] Level playlist is empty — no background music.", this);
                return;
            }
            EnsureMusicSource();
            playlistActive = true;
            playlistPos = 0;
            // Один валидный трек — зациклить его же (требование "следующий" без тишины).
            musicSource.loop = playlistQueue.Count == 1;
            PlayCurrentPlaylistTrack();
        }

        private void PlayCurrentPlaylistTrack()
        {
            if (playlistPos < 0 || playlistPos >= playlistQueue.Count) return;
            var trackIndex = playlistQueue[playlistPos];
            if (trackIndex < 0 || trackIndex >= levelPlaylist.Count) return;
            var track = levelPlaylist[trackIndex];
            if (track == null || track.clip == null) { NextTrack(); return; }
            EnsureMusicSource();
            musicSource.loop = playlistQueue.Count == 1;
            musicSource.clip = track.clip;
            currentEffectiveVolume = Mathf.Clamp01(globalMusicVolume) * Mathf.Clamp01(track.volume);
            musicSource.volume = currentEffectiveVolume;
            lastTrackStartUnscaled = Time.unscaledTime;
            musicSource.Play();
        }

        private void NextTrack()
        {
            if (!playlistActive || playlistQueue.Count == 0) return;
            playlistPos++;
            if (playlistPos >= playlistQueue.Count)
            {
                // Очередь исчерпана — новое перемешивание (без повтора границы:
                // пересобранная очередь снова случайна, этого достаточно для v1).
                playlistQueue = BuildQueueForTracks(levelPlaylist, UnityEngine.Random.Range(int.MinValue, int.MaxValue));
                if (playlistQueue.Count == 0) { playlistActive = false; return; }
                playlistPos = 0;
                if (musicSource != null)
                    musicSource.loop = playlistQueue.Count == 1;
            }
            PlayCurrentPlaylistTrack();
        }

        private void EnsureMusicSource()
        {
            if (musicSource == null)
            {
                // Отдельный источник: фейд музыки не должен глушить клики PlayOneShot.
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.playOnAwake = false;
            }
            // Пауза с миром (решение v2): музыка встает вместе с миром через
            // AudioListener.pause в Briefing/Paused/Win.
            musicSource.ignoreListenerPause = false;
        }

        /// <summary>
        /// Чистая функция: индексы валидных треков (clip назначен, длина > 0),
        /// перемешанные Фишером-Йетсом на seed. Без повторов, детерминирована.
        /// </summary>
        public static System.Collections.Generic.List<int> BuildQueueForTracks(
            System.Collections.Generic.IList<LevelMusicTrack> tracks, int seed)
        {
            var idx = new System.Collections.Generic.List<int>();
            if (tracks == null) return idx;
            for (var i = 0; i < tracks.Count; i++)
            {
                var t = tracks[i];
                if (t == null || t.clip == null) continue;
                if (t.clip.length <= 0f) continue;
                idx.Add(i);
            }
            return BuildShuffledQueue(idx, seed);
        }

        /// <summary>Чистая функция: перемешивание готового списка индексов.</summary>
        public static System.Collections.Generic.List<int> BuildShuffledQueue(
            System.Collections.Generic.IList<int> indices, int seed)
        {
            var q = new System.Collections.Generic.List<int>();
            if (indices == null) return q;
            foreach (var i in indices) q.Add(i);
            var rng = new System.Random(seed);
            for (var i = q.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                (q[i], q[j]) = (q[j], q[i]);
            }
            return q;
        }

        /// <summary>Тест-хук: подмена плейлиста без Inspector (только тесты/дебаг).</summary>
        public void OverridePlaylist(System.Collections.Generic.IList<LevelMusicTrack> tracks)
        {
            levelPlaylist = tracks != null
                ? new System.Collections.Generic.List<LevelMusicTrack>(tracks)
                : new System.Collections.Generic.List<LevelMusicTrack>();
        }

        public bool IsPlaylistActive => playlistActive;
        public AudioClip CurrentMusicClip => musicSource != null ? musicSource.clip : null;
        public float CurrentMusicVolume => musicSource != null ? musicSource.volume : 0f;
        public float GlobalMusicVolume => Mathf.Clamp01(globalMusicVolume);

        /// <summary>
        /// Плавно гасит музыку до нуля за fadeSeconds и останавливает.
        /// Ждёт в unscaled-времени — переходы живут при timeScale = 0.
        /// </summary>
        public void StopMusic(float fadeSeconds = 1f)
        {
            playlistActive = false;
            if (musicSource == null || !musicSource.isPlaying) return;
            StartCoroutine(FadeMusicOut(Mathf.Max(0f, fadeSeconds)));
        }

        private System.Collections.IEnumerator FadeMusicOut(float fadeSeconds)
        {
            float startVolume = musicSource != null ? musicSource.volume : 0f;
            if (fadeSeconds <= 0f)
            {
                if (musicSource != null)
                {
                    musicSource.Stop();
                    musicSource.volume = currentEffectiveVolume;
                }
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
                musicSource.volume = currentEffectiveVolume;
            }
        }

        public void PlaySwing()
        {
            if (playerSwing != null && source != null)
            {
                source.PlayOneShot(playerSwing);
                return;
            }
            PlayStub("swing");
        }

        public void PlayEnemyAttack()
        {
            if (enemySwing != null && source != null)
            {
                source.PlayOneShot(enemySwing);
                return;
            }
            PlayStub("enemySwing");
        }
        public void PlayFootsteps() => PlayFootstepsConfigured();

        public void PlayHit()
        {
            if (hit != null && source != null)
            {
                source.PlayOneShot(hit);
                return;
            }
            PlayStub("hit");
        }
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

        /// <summary>Null-safe старт плейлиста уровня (единственный триггер — DismissBriefing).</summary>
        public static void StartLevelPlaylist()
        {
            if (Instance != null)
                Instance.PlayLevelPlaylist();
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
        /*private void PlayCombatConfigured(string stubId, bool isPlayer)
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
        }*/

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
