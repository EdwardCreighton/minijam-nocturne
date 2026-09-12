using UnityEngine;

namespace Nocturne.Player
{
    /// <summary>
    /// Code-driven animator: the MainCharacter controller is a flat list of 16
    /// single-frame states with no transitions, so we Play() them directly by name.
    /// Facing is the last Move direction quantized to 4 ways; attacks alternate
    /// Attack1/Attack2 and overlay locomotion for a short window.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public sealed class PlayerVisual : MonoBehaviour
    {
        private const float AttackDisplayTime = 0.25f;

        private Animator animator;
        private PlayerController controller;
        private float attackTimer;
        private string attackClip = "Attack1";
        private string shownState = string.Empty;
        private bool alternate;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            animator = GetComponentInChildren<Animator>();
        }

        private void Update()
        {
            if (animator == null || controller == null) return;
            if (attackTimer > 0f) attackTimer -= Time.deltaTime;

            var dir = DirectionName(controller.LastDirection);
            string state;
            if (attackTimer > 0f)
                state = $"MainCharacter_{attackClip}{dir}";
            else
                state = controller.IsMoving ? $"MainCharacter_Run{dir}" : $"MainCharacter_Idle{dir}";

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

        private static string DirectionName(Vector2 dir)
        {
            if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
                return dir.x >= 0f ? "Right" : "Left";
            return dir.y >= 0f ? "Up" : "Down";
        }
    }
}
