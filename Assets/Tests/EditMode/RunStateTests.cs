using Nocturne.Core;
using NUnit.Framework;

namespace Nocturne.Tests.EditMode
{
    /// <summary>TZ §12.1: RunState economy rules.</summary>
    public sealed class RunStateTests
    {
        [Test]
        public void TrySpend_MovesUnspentToSpent_AndRecordsGate()
        {
            var run = new RunState();
            run.AddKill(100);
            Assert.IsTrue(run.TrySpend(30, "gate_east"));
            Assert.AreEqual(70, run.Unspent);
            Assert.AreEqual(30, run.SpentTotal);
            Assert.IsTrue(run.OpenedGateIds.Contains("gate_east"));
        }

        [Test]
        public void TrySpend_RefusesShortFunds_AndDuplicateGate()
        {
            var run = new RunState();
            run.AddKill(20);
            Assert.IsFalse(run.TrySpend(30, "gate_east"));
            Assert.AreEqual(20, run.Unspent);
            Assert.AreEqual(0, run.SpentTotal);

            run.AddKill(100);
            Assert.IsTrue(run.TrySpend(30, "gate_east"));
            Assert.IsFalse(run.TrySpend(30, "gate_east"), "double spend on same gate");
            Assert.AreEqual(90, run.Unspent);
            Assert.AreEqual(30, run.SpentTotal);
        }

        [Test]
        public void ResetAttempt_WipesOnlyUnspent()
        {
            var run = new RunState();
            run.AddKill(100);
            run.TrySpend(30, "gate_east");
            run.RegisterDeath();
            run.ResetAttempt();
            Assert.AreEqual(0, run.Unspent);
            Assert.AreEqual(30, run.SpentTotal, "spent never decreases");
            Assert.AreEqual(1, run.Deaths);
            Assert.IsTrue(run.OpenedGateIds.Contains("gate_east"));
        }

        [Test]
        public void AddKill_IgnoresNonPositive()
        {
            var run = new RunState();
            run.AddKill(0);
            run.AddKill(-5);
            Assert.AreEqual(0, run.Unspent);
        }
    }
}
