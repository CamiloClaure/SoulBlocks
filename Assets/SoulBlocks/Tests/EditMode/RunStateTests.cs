using NUnit.Framework;
using UnityEngine;
using SoulBlocks;

namespace SoulBlocks.Tests
{
    public class RunStateTests
    {
        [Test]
        public void FreshRunStateHasNoNullLists()
        {
            var run = new RunState();
            Assert.IsNotNull(run.unlocks);
            Assert.IsNotNull(run.upgrades);
            Assert.IsNotNull(run.prisons);
            Assert.IsNotNull(run.discoveredTiles);
            Assert.IsNotNull(run.specialBlockLevels);
            Assert.IsNotNull(run.plan);
            Assert.IsNotNull(run.plan.offered);
            Assert.IsNotNull(run.activeBlessings);
            Assert.AreEqual(5, run.brokenByTier.Length);
        }

        [Test]
        public void HasUnlockAndAddUnlock()
        {
            var run = new RunState();
            Assert.IsFalse(run.HasUnlock(UnlockIds.Raiz1));
            Assert.IsTrue(run.AddUnlock(UnlockIds.Raiz1));
            Assert.IsTrue(run.HasUnlock(UnlockIds.Raiz1));
            Assert.IsFalse(run.AddUnlock(UnlockIds.Raiz1));
        }

        [Test]
        public void UpgradeLevelGetSet()
        {
            var run = new RunState();
            Assert.AreEqual(0, run.UpgradeLevel("cargo"));
            run.SetUpgradeLevel("cargo", 2);
            Assert.AreEqual(2, run.UpgradeLevel("cargo"));
            run.SetUpgradeLevel("cargo", 3);
            Assert.AreEqual(3, run.UpgradeLevel("cargo"));
            Assert.AreEqual(1, run.upgrades.Count);
        }

        [Test]
        public void PrisonGetOrCreate()
        {
            var run = new RunState();
            var first = run.Prison(5);
            first.damage = 2;
            var again = run.Prison(5);
            Assert.AreSame(first, again);
            Assert.AreEqual(2, again.damage);
            Assert.AreEqual(1, run.prisons.Count);
        }

        [Test]
        public void JsonRoundTripKeepsNestedState()
        {
            var run = new RunState();
            run.seed = 123;
            run.AddUnlock(UnlockIds.Raiz1);
            run.SetUpgradeLevel("cargo", 4);
            run.Prison(2).damage = 5;
            run.Prison(2).broken = true;
            run.pickaxeLevel = 7;
            run.discoveredTiles.Add(9);
            run.plan.offered.Add(new ObjectiveRecord { kind = "collect", targetId = 1, goal = 10, progress = 3 });
            run.plan.chosenObjective = 0;
            run.plan.anomaly = new AnomalyRecord { kind = "swarm", branchId = 1, position = new Vector3(4, 0, 4), consumed = false };
            run.activeBlessings.Add("dash_extra");

            string json = JsonUtility.ToJson(run);
            var loaded = JsonUtility.FromJson<RunState>(json);

            Assert.AreEqual(123, loaded.seed);
            Assert.IsTrue(loaded.HasUnlock(UnlockIds.Raiz1));
            Assert.AreEqual(4, loaded.UpgradeLevel("cargo"));
            Assert.AreEqual(5, loaded.Prison(2).damage);
            Assert.IsTrue(loaded.Prison(2).broken);
            Assert.AreEqual(7, loaded.pickaxeLevel);
            CollectionAssert.Contains(loaded.discoveredTiles, 9);
            Assert.AreEqual(1, loaded.plan.offered.Count);
            Assert.AreEqual(0, loaded.plan.chosenObjective);
            Assert.AreEqual(10, loaded.plan.offered[0].goal);
            Assert.IsNotNull(loaded.plan.anomaly);
            Assert.AreEqual("swarm", loaded.plan.anomaly.kind);
            CollectionAssert.Contains(loaded.activeBlessings, "dash_extra");
        }
    }
}
