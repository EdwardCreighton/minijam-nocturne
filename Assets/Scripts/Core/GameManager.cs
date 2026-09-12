using UnityEngine;
using UnityEngine.InputSystem;

namespace Nocturne.Core
{
    public enum GameState
    {
        Briefing,
        Playing,
        Paused,
        Dying,
        Win,
    }

    /// <summary>
    /// Composition root of GameLevel (TZ §9, §10): owns the RunState for the whole run,
    /// the GameState machine, gate registry, generated-input lifetime and pause.
    /// Death flow lands in P2, enemy reset in P3, win flow in P8.
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Tooltip("Assigned by setup; all balance numbers come from here.")]
        public Config.BalanceConfig config;

        public RunState Run { get; private set; } = new();
        public GameState State { get; private set; } = GameState.Playing;

        public InputSystem_Actions Input { get; private set; }

        private readonly System.Collections.Generic.List<World.Gate> gates = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Layers.ApplyCollisionMatrix();

            Input = new InputSystem_Actions();
            Input.Player.Enable();
            Input.Player.Pause.performed += OnPausePerformed;
        }

        private void Start()
        {
            // TODO P8: enter Briefing and wait for dismiss instead.
            State = GameState.Playing;
        }

        private void Update()
        {
            if (State == GameState.Playing)
                Run.AddRunTime(Time.deltaTime);
        }

        private void OnDestroy()
        {
            // Never leak paused state into another scene (e.g. back to MainMenu).
            Time.timeScale = 1f;
            AudioListener.pause = false;

            if (Input != null)
            {
                Input.Player.Pause.performed -= OnPausePerformed;
                Input.Dispose();
                Input = null;
            }

            if (Instance == this)
                Instance = null;
        }

        public void RegisterGate(World.Gate gate)
        {
            if (gate != null && !gates.Contains(gate))
                gates.Add(gate);
        }

        public void TogglePause()
        {
            if (State == GameState.Playing)
            {
                State = GameState.Paused;
                Time.timeScale = 0f;
                AudioListener.pause = true;
                Input.UI.Enable();
            }
            else if (State == GameState.Paused)
            {
                State = GameState.Playing;
                Time.timeScale = 1f;
                AudioListener.pause = false;
                Input.UI.Disable();
            }
            // Briefing/Dying/Win: Pause is ignored (TZ §9).
        }

        private void OnPausePerformed(InputAction.CallbackContext _)
        {
            TogglePause();
        }

        /// <summary>
        /// Player death entry point (called by PlayerHealth). Runs the P2 death flow:
        /// unscaled delay → wipe Unspent → respawn at Start with full HP → resume.
        /// Enemy reset lands in P3 (TODO).
        /// </summary>
        public void OnPlayerDied()
        {
            if (State != GameState.Playing) return;
            State = GameState.Dying;
            StartCoroutine(DeathRoutine());
        }

        private System.Collections.IEnumerator DeathRoutine()
        {
            var delay = config != null ? config.deathDelay : 1f;
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, delay));

            Run.ResetAttempt();
            Run.RegisterDeath();

            var resetter = GetComponent<World.AttemptResetter>();
            var spawn = resetter != null ? resetter.RespawnPosition() : Vector3.zero;

            var playerGo = GameObject.FindWithTag("Player");
            if (playerGo != null)
            {
                var rb = playerGo.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.position = spawn;
                    rb.linearVelocity = Vector2.zero;
                }
                else
                {
                    playerGo.transform.position = spawn;
                }

                var health = playerGo.GetComponent<Player.PlayerHealth>();
                if (health != null) health.ResetHP();
                var interactor = playerGo.GetComponent<Player.PlayerInteractor>();
                if (interactor != null) interactor.CancelHold();
            }

            // Full-HP recreation at initial points; gates are untouched (TZ §9).
            foreach (var spawner in FindObjectsByType<Enemies.EnemySpawner>(FindObjectsSortMode.None))
                spawner.RespawnAll();

            var cam = FindFirstObjectByType<FollowCam>();
            if (cam != null) cam.Snap();

            State = GameState.Playing;
        }
    }
}
