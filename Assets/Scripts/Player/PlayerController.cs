using Nocturne.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Nocturne.Player
{
    /// <summary>
    /// Top-down kinematic movement (TZ §5.1). Reads the generated-input Move action,
    /// tracks the last non-zero direction (attack facing + animation).
    /// Moves only while GameState is Playing.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        public Vector2 LastDirection { get; private set; } = Vector2.down;
        public bool IsMoving { get; private set; }

        private Rigidbody2D rb;
        private float stepTimer;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        private void FixedUpdate()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Playing || gm.Input == null)
            {
                IsMoving = false;
                return;
            }

            var move = gm.Input.Player.Move.ReadValue<Vector2>();
            if (move.sqrMagnitude > 1f) move.Normalize();
            IsMoving = move.sqrMagnitude > 0.0001f;
            if (IsMoving) LastDirection = move.normalized;

            var speed = gm.config != null ? gm.config.moveSpeed : 5f;
            MovementUtil.TryMove(rb, move * speed * Time.fixedDeltaTime);

            if (IsMoving)
            {
                stepTimer -= Time.fixedDeltaTime;
                if (stepTimer <= 0f)
                {
                    var interval = gm.config != null ? gm.config.footstepsInterval : 0.25f;
                    stepTimer = Mathf.Max(0.05f, interval);
                    AudioManager.Footsteps();
                }
            }
            else
            {
                stepTimer = 0f;
            }
        }
    }
}
