using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nocturne.Config
{
    /// <summary>
    /// Single source of truth for gameplay balance (TZ §5.2, §6.1, §8.2).
    /// Default asset lives at Assets/Settings/BalanceConfig.asset.
    /// Hold duration (holdTime) is measured by code, NOT by an Input interaction (TZ §2).
    /// </summary>
    [CreateAssetMenu(fileName = "BalanceConfig", menuName = "Nocturne/BalanceConfig")]
    public sealed class BalanceConfig : ScriptableObject
    {
        [Header("Player")]
        public float moveSpeed = 5f;
        public int maxHP = 100;

        [Header("Attack")]
        public int attackDamage = 25;
        public float attackCooldown = 0.35f;
        public float attackArc = 90f;
        public float attackRange = 1.2f;

        [Header("Survivability")]
        [Tooltip("Invulnerability window after taking any damage.")]
        public float damageCooldown = 0.5f;

        [Header("Interact (measured by code)")]
        public float holdTime = 0.6f;
        public float interactRadius = 1.5f;
        public float gateDebounce = 0.5f;
        public float deathDelay = 1f;

        [Header("Difficulty = f(SpentTotal)")]
        public int difficultyN = 100;
        public float difficultyK = 0.25f;
        public float difficultyM = 0.15f;
        public int maxLevel = 5;
        public List<TierEntry> tierTable = new();

        [Header("Enemies (Chaser = P3 weak type)")]
        public int chaserHP = 50;
        public int chaserDamage = 10;
        public int chaserScore = 10;
        public float enemySpeed = 2.5f;
        public float enemyAggroRadius = 6f;
        public float enemyAttackRadius = 1f;
        public float enemyAttackCooldown = 1f;

        private void OnValidate()
        {
            moveSpeed = Mathf.Max(0.1f, moveSpeed);
            maxHP = Mathf.Max(1, maxHP);
            attackDamage = Mathf.Max(1, attackDamage);
            attackCooldown = Mathf.Max(0.05f, attackCooldown);
            attackArc = Mathf.Clamp(attackArc, 1f, 360f);
            attackRange = Mathf.Max(0.1f, attackRange);
            damageCooldown = Mathf.Max(0f, damageCooldown);
            holdTime = Mathf.Max(0.05f, holdTime);
            interactRadius = Mathf.Max(0.1f, interactRadius);
            gateDebounce = Mathf.Max(0f, gateDebounce);
            deathDelay = Mathf.Max(0f, deathDelay);
            difficultyN = Mathf.Max(1, difficultyN);
            difficultyK = Mathf.Max(0f, difficultyK);
            difficultyM = Mathf.Max(0f, difficultyM);
            maxLevel = Mathf.Max(1, maxLevel);
            chaserHP = Mathf.Max(1, chaserHP);
            chaserDamage = Mathf.Max(1, chaserDamage);
            chaserScore = Mathf.Max(0, chaserScore);
            enemySpeed = Mathf.Max(0.1f, enemySpeed);
            enemyAggroRadius = Mathf.Max(0.5f, enemyAggroRadius);
            enemyAttackRadius = Mathf.Max(0.2f, enemyAttackRadius);
            enemyAttackCooldown = Mathf.Max(0.1f, enemyAttackCooldown);
        }

        [Serializable]
        public sealed class TierEntry
        {
            public int level = 1;
            public List<GameObject> enemyPrefabs = new();
        }
    }
}
