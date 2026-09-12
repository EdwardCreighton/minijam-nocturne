using System;
using System.Collections.Generic;

namespace Nocturne.Core
{
    /// <summary>
    /// Sole owner of run economy and progress (TZ §9). Plain C# class (no MonoBehaviour)
    /// so EditMode tests can instantiate it directly. Owned by GameManager.
    /// Points: kills -> Unspent, gates -> Unspent -= cost / SpentTotal += cost,
    /// death -> Unspent = 0. SpentTotal never decreases.
    /// </summary>
    public sealed class RunState
    {
        public int Unspent { get; private set; }
        public int SpentTotal { get; private set; }
        public HashSet<string> OpenedGateIds { get; } = new();
        public int Deaths { get; private set; }
        public float RunTime { get; private set; }
        public string FinishId { get; private set; } = string.Empty;

        public event Action Changed;

        public void AddKill(int points)
        {
            if (points <= 0) return;
            Unspent += points;
            Changed?.Invoke();
        }

        public bool TrySpend(int cost, string gateId)
        {
            if (cost <= 0 || string.IsNullOrEmpty(gateId)) return false;
            if (OpenedGateIds.Contains(gateId)) return false;
            if (Unspent < cost) return false;
            Unspent -= cost;
            SpentTotal += cost;
            OpenedGateIds.Add(gateId);
            Changed?.Invoke();
            return true;
        }

        /// <summary>Death: only Unspent is wiped (TZ §9 table).</summary>
        public void ResetAttempt()
        {
            Unspent = 0;
            Changed?.Invoke();
        }

        public void RegisterDeath()
        {
            Deaths++;
            Changed?.Invoke();
        }

        public void RegisterWin(string finishId)
        {
            FinishId = finishId ?? string.Empty;
            Changed?.Invoke();
        }

        /// <summary>Called by GameManager every frame while Playing. No event (per-frame).</summary>
        public void AddRunTime(float dt)
        {
            if (dt > 0f) RunTime += dt;
        }

        public void FullReset()
        {
            Unspent = 0;
            SpentTotal = 0;
            OpenedGateIds.Clear();
            Deaths = 0;
            RunTime = 0f;
            FinishId = string.Empty;
            Changed?.Invoke();
        }
    }
}
