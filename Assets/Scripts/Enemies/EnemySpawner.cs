using System;
using System.Collections.Generic;
using Nocturne.Core;
using UnityEngine;

namespace Nocturne.Enemies
{
    /// <summary>
    /// Manual spawn-point registry + full reset on player death (TZ §6.1):
    /// all enemies are recreated at their initial points with full HP, which
    /// re-enables farming the opened area (Concept §6–8).
    /// Optional timed trickle-respawn (respawnDelay &gt; 0) is capped by maxAlive.
    /// </summary>
    public sealed class EnemySpawner : MonoBehaviour
    {
        [Serializable]
        public sealed class SpawnEntry
        {
            public GameObject prefab;
            public Vector3 position;
            [Tooltip("If set and current difficulty level >= tierMinLevel, spawn this instead.")]
            public GameObject tierPrefab;
            [Min(1)] public int tierMinLevel = 2;
        }

        public List<SpawnEntry> entries = new();
        [Min(0f)] public float respawnDelay;
        [Min(1)] public int maxAlive = 20;

        private readonly List<Enemy> alive = new();
        private readonly Dictionary<Enemy, SpawnEntry> origin = new();
        private readonly List<SpawnEntry> deadQueue = new();
        private float respawnTimer;

        private void Start()
        {
            SpawnAll();
        }

        private void Update()
        {
            if (respawnDelay <= 0f) return;
            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Playing) return;
            if (deadQueue.Count == 0 || alive.Count >= maxAlive) return;

            respawnTimer += Time.deltaTime;
            if (respawnTimer < respawnDelay) return;
            respawnTimer = 0f;

            var entry = deadQueue[0];
            deadQueue.RemoveAt(0);
            SpawnOne(entry);
        }

        /// <summary>Death flow: wipe live enemies and recreate the initial set.</summary>
        public void RespawnAll()
        {
            foreach (var enemy in alive)
            {
                if (enemy != null)
                {
                    enemy.Died -= OnEnemyDied;
                    Destroy(enemy.gameObject);
                }
            }

            alive.Clear();
            origin.Clear();
            deadQueue.Clear();
            respawnTimer = 0f;
            SpawnAll();
        }

        private void SpawnAll()
        {
            foreach (var entry in entries)
                SpawnOne(entry);
        }

        private void SpawnOne(SpawnEntry entry)
        {
            if (entry == null || entry.prefab == null) return;

            // Difficulty is evaluated per spawn (never re-scaled live): stats mults
            // plus group composition via tierPrefab (TZ §8.2).
            var gm = GameManager.Instance;
            var cfg = gm != null ? gm.config : null;
            var spent = gm != null ? gm.Run.SpentTotal : 0;
            var level = DifficultyScaler.GetLevel(spent, cfg);
            var prefab = entry.tierPrefab != null && level >= Mathf.Max(1, entry.tierMinLevel)
                ? entry.tierPrefab
                : entry.prefab;

            var go = Instantiate(prefab, entry.position, Quaternion.identity);
            var enemy = go.GetComponent<Enemy>();
            if (enemy == null)
            {
                Debug.LogError($"EnemySpawner: prefab {prefab.name} has no Enemy.", this);
                Destroy(go);
                return;
            }

            enemy.Initialize(
                DifficultyScaler.GetHpMult(level, cfg),
                DifficultyScaler.GetDmgMult(level, cfg));

            alive.Add(enemy);
            origin[enemy] = entry;
            enemy.Died += OnEnemyDied;
        }

        private void OnEnemyDied(Enemy enemy)
        {
            alive.Remove(enemy);
            if (origin.TryGetValue(enemy, out var entry))
            {
                origin.Remove(enemy);
                if (entry != null)
                    deadQueue.Add(entry);
            }
        }
    }
}
