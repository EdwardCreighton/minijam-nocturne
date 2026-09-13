using Nocturne.Combat;
using Nocturne.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Nocturne.Player
{
    /// <summary>
    /// Melee swing on Attack (TZ §5.1–5.2): arc in front of the player along the last
    /// Move direction, cooldown-gated, one instantaneous overlap query per swing
    /// (each victim has a single collider, so one hit per swing is structural).
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(PlayerVisual))]
    public sealed class PlayerCombat : MonoBehaviour
    {
        private PlayerController controller;
        private PlayerVisual visual;
        private float nextSwingTime;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            visual = GetComponent<PlayerVisual>();
        }

        // Subscribed in Start: GameManager.Awake (which creates Input) always runs first.
        private void Start()
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.Input != null)
                gm.Input.Player.Attack.performed += OnAttackPerformed;
        }

        private void OnDisable()
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.Input != null)
                gm.Input.Player.Attack.performed -= OnAttackPerformed;
        }

        private void OnAttackPerformed(InputAction.CallbackContext _)
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Playing) return;
            if (Time.time < nextSwingTime) return;

            var cfg = gm.config;
            var cooldown = cfg != null ? cfg.attackCooldown : 0.35f;
            nextSwingTime = Time.time + cooldown;

            var dir = controller.LastDirection;
            var range = cfg != null ? cfg.attackRange : 1.2f;
            var arc = cfg != null ? cfg.attackArc : 90f;
            var damage = cfg != null ? cfg.attackDamage : 25;

            var origin = (Vector2)transform.position;
            var hits = Physics2D.OverlapCircleAll(origin, range, 1 << Layers.Enemy);
            var halfArcCos = Mathf.Cos(arc * 0.5f * Mathf.Deg2Rad);
            var landed = false;
            foreach (var hit in hits)
            {
                var toTarget = ((Vector2)hit.transform.position - origin).normalized;
                if (Vector2.Dot(dir, toTarget) < halfArcCos) continue;
                var damageable = hit.GetComponent<IDamageable>();
                if (damageable == null) continue;
                damageable.TakeDamage(damage, origin);
                landed = true;
            }

            if (landed)
            {
                var cam = FindFirstObjectByType<FollowCam>();
                if (cam != null)
                    cam.Shake(
                        cfg != null ? cfg.hitShakeAmplitude : 0.15f,
                        cfg != null ? cfg.hitShakeDuration : 0.2f);
            }

            visual.PlayAttack();
            AudioManager.Swing();
        }
    }
}
