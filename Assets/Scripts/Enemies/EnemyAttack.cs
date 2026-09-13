using Nocturne.Combat;
using Nocturne.Core;
using Nocturne.Player;
using UnityEngine;

namespace Nocturne.Enemies
{
    /// <summary>
    /// Contact damage (TZ §6.1): distance-based check (both sides are kinematic,
    /// so collision callbacks would not fire), cooldown-gated per enemy.
    /// Hits only while GameState is Playing.
    /// </summary>
    [RequireComponent(typeof(Enemy))]
    public sealed class EnemyAttack : MonoBehaviour
    {
        private Enemy body;
        private PlayerHealth playerHealth;
        private Transform player;
        private float nextHitTime;

        private void Awake()
        {
            body = GetComponent<Enemy>();
            var playerGo = GameObject.FindWithTag("Player");
            if (playerGo != null)
            {
                player = playerGo.transform;
                playerHealth = playerGo.GetComponent<PlayerHealth>();
            }
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Playing) return;
            if (!body.IsAlive || player == null || playerHealth == null) return;
            if (Time.time < nextHitTime) return;

            var cfg = gm.config;
            var radius = cfg != null ? cfg.enemyAttackRadius : 1f;
            if (Vector2.Distance(transform.position, player.position) > radius) return;

            var cooldown = cfg != null ? cfg.enemyAttackCooldown : 1f;
            nextHitTime = Time.time + cooldown;
            playerHealth.TakeDamage(body.Damage, transform.position);
            AudioManager.EnemyAttack();
            var visual = GetComponent<EnemyVisual>();
            if (visual != null) visual.PlayAttack();
        }
    }
}
