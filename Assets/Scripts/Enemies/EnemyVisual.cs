using UnityEngine;

namespace Nocturne.Enemies
{
    /// <summary>
    /// Code-driven animator for enemies: drives the same shared MainCharacter
    /// 16-state controller (flat list, no transitions) via Animator.Play(),
    /// with the same state naming as PlayerVisual.
    /// Facing/moving derive from actual displacement (no input to read);
    /// attack flashes come from EnemyAttack. Self-attached by Enemy.Awake,
    /// so existing prefabs need no setup.
    /// </summary>
    public sealed class EnemyVisual : MonoBehaviour
    {
        private const float AttackDisplayTime = 0.25f;
        // Smoothed-speed gates for Run/Idle (hysteresis: harder to start than to stop).
        private const float RunEnterSpeed = 0.8f;
        private const float RunExitSpeed = 0.4f;
        // Low-pass rate for velocity (1/s): kills per-frame jitter, stays responsive.
        private const float SmoothRate = 10f;
        // A new axis must beat the current one by this factor to switch facing,
        // so diagonal movement holds one direction instead of flipping each frame.
        private const float AxisBias = 1.25f;

        private Animator animator;
        private Vector2 lastDirection = Vector2.down;
        private Vector2 smoothVel;
        private Vector3 prevPosition;
        private bool isMoving;
        private float attackTimer;
        private string attackClip = "Attack1";
        private string shownState = string.Empty;
        private bool alternate;

        private void Awake()
        {
            animator = GetComponentInChildren<Animator>();
            prevPosition = transform.position;
        }

        private void Update()
        {
            if (animator == null) return;
            if (attackTimer > 0f) attackTimer -= Time.deltaTime;

            var dt = Time.deltaTime;
            var delta = (Vector2)(transform.position - prevPosition);
            prevPosition = transform.position;
            var instant = dt > 0f ? delta / dt : Vector2.zero;
            smoothVel = Vector2.Lerp(smoothVel, instant, 1f - Mathf.Exp(-SmoothRate * Mathf.Max(0f, dt)));
            var speed = smoothVel.magnitude;

            if (isMoving)
            {
                if (speed < RunExitSpeed) isMoving = false;
            }
            else if (speed > RunEnterSpeed)
            {
                isMoving = true;
            }
            if (isMoving) lastDirection = QuantizeWithBias(smoothVel, lastDirection);

            var dir = DirectionName(lastDirection);
            string state;
            if (attackTimer > 0f)
                state = $"MainCharacter_{attackClip}{dir}";
            else
                state = isMoving ? $"MainCharacter_Run{dir}" : $"MainCharacter_Idle{dir}";

            if (state != shownState)
            {
                shownState = state;
                animator.Play(state);
            }
        }

        public void PlayAttack()
        {
            alternate = !alternate;
            attackClip = alternate ? "Attack1" : "Attack2";
            attackTimer = AttackDisplayTime;
            shownState = string.Empty; // force replay even if same clip twice in a row
        }

        /// <summary>
        /// 4-way facing with a bias toward the current axis: switching axes
        /// needs a clear winner, so near-diagonal movement holds steady
        /// instead of flickering between two states every frame.
        /// </summary>
        private static Vector2 QuantizeWithBias(Vector2 v, Vector2 current)
        {
            var ax = Mathf.Abs(v.x);
            var ay = Mathf.Abs(v.y);
            if (Mathf.Abs(current.x) >= Mathf.Abs(current.y))
            {
                if (ay > ax * AxisBias) return v.y >= 0f ? Vector2.up : Vector2.down;
                if (ax < 0.0001f) return current;
                return v.x >= 0f ? Vector2.right : Vector2.left;
            }
            if (ax > ay * AxisBias) return v.x >= 0f ? Vector2.right : Vector2.left;
            if (ay < 0.0001f) return current;
            return v.y >= 0f ? Vector2.up : Vector2.down;
        }

        private static string DirectionName(Vector2 dir)
        {
            if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
                return dir.x >= 0f ? "Right" : "Left";
            return dir.y >= 0f ? "Up" : "Down";
        }
    }
}
