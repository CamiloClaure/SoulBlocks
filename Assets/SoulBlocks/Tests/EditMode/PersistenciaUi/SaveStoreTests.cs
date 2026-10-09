using NUnit.Framework;
using UnityEngine;

namespace SoulBlocks.Tests
{
    public class SaveStoreTests
    {
        static RunState BuildFullRun()
        {
            var run = new RunState();
            run.seed = 123;
            run.effectiveSeed = 124;
            run.expeditionIndex = 3;
            run.playTimeSeconds = 90.5f;
            run.inExpedition = true;
            run.victory = false;
            run.reserveSouls = 40;
            run.cargoSouls = 12;
            run.wisdom = 7;
            run.energy = 18.5f;
            run.treeStage = 2;
            run.AddUnlock(UnlockIds.Raiz1);
            run.AddUnlock(UnlockIds.Revelacion);
            run.SetUpgradeLevel("carga", 3);
            run.Prison(5).damage = 4;
            run.Prison(5).phasesDone = 1;
            run.Prison(9).broken = true;
            run.brokenByTier[(int)PrisonTier.T1] = 2;
            run.discoveredTiles.Add(3);
            run.discoveredTiles.Add(9);
            run.groundSouls.Add(new GroundSoul { position = new Vector3(1, 0, 2), value = 5 });
            run.plan.offered.Add(new ObjectiveRecord { kind = "collect", targetId = 1, branchId = 0, goal = 10, progress = 4, rewardWisdom = 2 });
            run.plan.chosenObjective = 0;
            run.plan.anomaly = new AnomalyRecord { kind = "swarm", branchId = 1, position = new Vector3(4, 0, 4) };
            run.activeBlessings.Add("dash_extra");
            run.totalSoulsCollected = 55;
            run.totalPrisonsBroken = 1;
            return run;
        }

        [Test]
        public void RoundTripKeepsEverything()
        {
            var run = BuildFullRun();
            string json = SaveStore.Serialize(run);
            var loaded = SaveStore.Deserialize(json);

            Assert.IsNotNull(loaded);
            Assert.AreEqual(run.seed, loaded.seed);
            Assert.AreEqual(run.effectiveSeed, loaded.effectiveSeed);
            Assert.AreEqual(run.expeditionIndex, loaded.expeditionIndex);
            Assert.AreEqual(run.playTimeSeconds, loaded.playTimeSeconds, .0001f);
            Assert.AreEqual(run.inExpedition, loaded.inExpedition);
            Assert.AreEqual(run.reserveSouls, loaded.reserveSouls);
            Assert.AreEqual(run.cargoSouls, loaded.cargoSouls);
            Assert.AreEqual(run.wisdom, loaded.wisdom);
            Assert.AreEqual(run.treeStage, loaded.treeStage);
            Assert.IsTrue(loaded.HasUnlock(UnlockIds.Raiz1));
            Assert.IsTrue(loaded.HasUnlock(UnlockIds.Revelacion));
            Assert.AreEqual(3, loaded.UpgradeLevel("carga"));
            Assert.AreEqual(4, loaded.Prison(5).damage);
            Assert.AreEqual(1, loaded.Prison(5).phasesDone);
            Assert.IsTrue(loaded.Prison(9).broken);
            Assert.AreEqual(2, loaded.brokenByTier[(int)PrisonTier.T1]);
            CollectionAssert.AreEquivalent(run.discoveredTiles, loaded.discoveredTiles);
            Assert.AreEqual(1, loaded.groundSouls.Count);
            Assert.AreEqual(5, loaded.groundSouls[0].value);
            Assert.AreEqual(new Vector3(1, 0, 2), loaded.groundSouls[0].position);
            Assert.AreEqual(1, loaded.plan.offered.Count);
            Assert.AreEqual("collect", loaded.plan.offered[0].kind);
            Assert.AreEqual(0, loaded.plan.chosenObjective);
            Assert.IsNotNull(loaded.plan.anomaly);
            Assert.AreEqual("swarm", loaded.plan.anomaly.kind);
            CollectionAssert.Contains(loaded.activeBlessings, "dash_extra");
            Assert.AreEqual(55, loaded.totalSoulsCollected);
            Assert.AreEqual(1, loaded.totalPrisonsBroken);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("{ this is not json")]
        [TestCase("definitely not json at all")]
        public void CorruptOrEmptyJsonReturnsNull(string json)
        {
            Assert.IsNull(SaveStore.Deserialize(json));
        }

        [Test]
        public void WrongVersionReturnsNull()
        {
            var run = new RunState();
            run.version = SaveStore.CurrentVersion + 1;
            string json = SaveStore.Serialize(run);

            Assert.IsNull(SaveStore.Deserialize(json));
        }

        [Test]
        public void MatchingVersionRoundTrips()
        {
            var run = new RunState();
            run.version = SaveStore.CurrentVersion;
            string json = SaveStore.Serialize(run);

            Assert.IsNotNull(SaveStore.Deserialize(json));
        }
    }
}
