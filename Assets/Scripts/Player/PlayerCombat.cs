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
            var centerOffset = cfg != null ? cfg.attackCenterOffset : Vector2.zero;

            var origin = AttackOrigin((Vector2)transform.position, dir, centerOffset);
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

        /// <summary>
        /// Swing center from a facing-space offset (X = right of facing, Y = forward).
        /// Shared by the hit test and the gizmo so they can never disagree.
        /// </summary>
        private static Vector2 AttackOrigin(Vector2 playerPos, Vector2 dir, Vector2 offset)
        {
            var right = new Vector2(dir.y, -dir.x);
            return playerPos + dir * offset.y + right * offset.x;
        }

        /// <summary>
        /// Scene-view preview of the swing sector (range + arc along the current
        /// facing). Uses the live config at runtime, the scene GameManager's
        /// config in edit mode. Editor-only, stripped from builds.
        /// </summary>
        private void OnDrawGizmos()
        {
            var ctrl = controller != null ? controller : GetComponent<PlayerController>();
            var dir = Vector2.down;
            if (ctrl != null && ctrl.LastDirection.sqrMagnitude > 0.0001f)
                dir = ctrl.LastDirection.normalized;

            var gm = GameManager.Instance;
            if (gm == null)
                gm = FindFirstObjectByType<GameManager>();
            var cfg = gm != null ? gm.config : null;
            var range = cfg != null ? cfg.attackRange : 1.5f;
            var arc = cfg != null ? cfg.attackArc : 90f;
            var centerOffset = cfg != null ? cfg.attackCenterOffset : Vector2.zero;

            var origin = AttackOrigin((Vector2)transform.position, dir, centerOffset);
            var baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            var safeArc = Mathf.Clamp(arc, 1f, 360f);
            var half = safeArc * 0.5f;

            Vector2 Edge(float angleDeg)
            {
                var r = angleDeg * Mathf.Deg2Rad;
                return new Vector2(Mathf.Cos(r), Mathf.Sin(r));
            }

            Gizmos.color = new Color(1f, 0.35f, 0.25f, 1f);
            const int segments = 24;
            var prev = origin + Edge(baseAngle - half) * range;
            Gizmos.DrawLine(origin, prev);
            for (var i = 1; i <= segments; i++)
            {
                var p = origin + Edge(baseAngle - half + safeArc * i / segments) * range;
                Gizmos.DrawLine(prev, p);
                prev = p;
            }
            Gizmos.DrawLine(origin, prev);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(origin, origin + dir * range);
        }
    }
}
