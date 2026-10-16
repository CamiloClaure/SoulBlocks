using NUnit.Framework;
using UnityEngine;
using SoulBlocks;

namespace SoulBlocks.Tests
{
    public class ProgressionRulesTests
    {
        static UpgradeDef Carga => UpgradeCatalog.Get("carga");
        static UpgradeDef Dash => UpgradeCatalog.Get("dash");
        static UpgradeDef Raiz1 => UpgradeCatalog.Get("raiz1");
        static UpgradeDef Raiz2 => UpgradeCatalog.Get("raiz2");
        static UpgradeDef Raiz3 => UpgradeCatalog.Get("raiz3");
        static UpgradeDef Despertar => UpgradeCatalog.Get("despertar");
        static UpgradeDef Entrega => UpgradeCatalog.Get("entrega");

        [Test]
        public void CostGrowsGeometrically()
        {
            Assert.AreEqual(40, ProgressionRules.Cost(Carga, 1));
            Assert.AreEqual(60, ProgressionRules.Cost(Carga, 2));
            Assert.AreEqual(90, ProgressionRules.Cost(Carga, 3));
        }

        [Test]
        public void CostUsesPerUpgradeGrowth()
        {
            // dash: growth 1.5, base 60.
            Assert.AreEqual(60, ProgressionRules.Cost(Dash, 1));
            Assert.AreEqual(90, ProgressionRules.Cost(Dash, 2));
            Assert.AreEqual(135, ProgressionRules.Cost(Dash, 3));
        }

        [Test]
        public void CannotBuyWithoutCurrency()
        {
            var run = new RunState { reserveSouls = 0 };
            Assert.AreEqual("faltan 40 almas", ProgressionRules.WhyCannotBuy(run, Carga));
            Assert.IsFalse(ProgressionRules.Buy(run, Carga));
            Assert.AreEqual(0, run.UpgradeLevel("carga"));
        }

        [Test]
        public void CannotBuyAboveMaxLevel()
        {
            var run = new RunState { reserveSouls = 100000 };
            run.SetUpgradeLevel("carga", Carga.maxLevel);
            Assert.AreEqual("nivel máximo", ProgressionRules.WhyCannotBuy(run, Carga));
            Assert.IsFalse(ProgressionRules.Buy(run, Carga));
        }

        [Test]
        public void CannotBuyBeforeTierGate()
        {
            var run = new RunState { wisdom = 100 };
            Assert.AreEqual("requiere Raíz I", ProgressionRules.WhyCannotBuy(run, Raiz2));

            run.treeStage = 1; // raiz1 bought, T2 still unbroken
            run.SetUpgradeLevel("raiz1", 1);
            Assert.AreEqual("requiere romper una prisión II", ProgressionRules.WhyCannotBuy(run, Raiz2));
        }

        [Test]
        public void CoreNodesMustBeBoughtInOrder()
        {
            var run = new RunState { wisdom = 100 };
            run.brokenByTier[1] = 1; // T2 broken, but raiz1 not bought yet
            Assert.IsFalse(ProgressionRules.Buy(run, Raiz2));
            Assert.AreEqual(0, run.UpgradeLevel("raiz2"));
        }

        [Test]
        public void BuyingDeductsCurrencyAndLevels()
        {
            var run = new RunState { reserveSouls = 1000 };
            Assert.IsTrue(ProgressionRules.Buy(run, Carga));
            Assert.AreEqual(1, run.UpgradeLevel("carga"));
            Assert.AreEqual(1000 - 40, run.reserveSouls);

            Assert.IsTrue(ProgressionRules.Buy(run, Carga));
            Assert.AreEqual(2, run.UpgradeLevel("carga"));
            Assert.AreEqual(1000 - 40 - 60, run.reserveSouls);
        }

        [Test]
        public void ApplyStatsRaisesSpiritCapacityByOneCargaLevel()
        {
            var run = new RunState();
            run.SetUpgradeLevel("carga", 1);
            var stats = new StatSheet();
            ProgressionRules.ApplyStats(run, stats);
            Assert.AreEqual(Balance.SpiritCapacity + Balance.CargaEffect, stats.Get(StatId.SpiritCapacity), .0001f);
        }

        [Test]
        public void ApplyStatsGivesDashCharges2AtDashLevel3()
        {
            var run = new RunState();
            run.SetUpgradeLevel("dash", 3);
            var stats = new StatSheet();
            ProgressionRules.ApplyStats(run, stats);
            Assert.AreEqual(2f, stats.Get(StatId.DashCharges), .0001f);
            Assert.AreEqual(Balance.DashDistance + Balance.DashEffect * 3, stats.Get(StatId.DashDistance), .0001f);
        }

        [Test]
        public void ApplyStatsClearsSourceWhenLevelZero()
        {
            var run = new RunState();
            var stats = new StatSheet();
            run.SetUpgradeLevel("carga", 1);
            ProgressionRules.ApplyStats(run, stats);
            run.SetUpgradeLevel("carga", 0);
            ProgressionRules.ApplyStats(run, stats);
            Assert.AreEqual(Balance.SpiritCapacity, stats.Get(StatId.SpiritCapacity), .0001f);
        }

        [Test]
        public void CoreCompleteTrueWithOnlyTheFourCoreNodesBought()
        {
            var run = new RunState { wisdom = 1000 };
            BuyAllCoreInOrder(run);
            Assert.IsTrue(ProgressionRules.CoreComplete(run));
            // Optional nodes never required.
            Assert.AreEqual(0, run.UpgradeLevel("entrega"));
        }

        [Test]
        public void TreeStageFollowsCoreNodes()
        {
            var run = new RunState { wisdom = 1000 };
            Assert.AreEqual(0, run.treeStage);

            run.brokenByTier[0] = 1;
            ProgressionRules.Buy(run, Raiz1);
            Assert.AreEqual(1, run.treeStage);

            run.brokenByTier[1] = 1;
            ProgressionRules.Buy(run, Raiz2);
            Assert.AreEqual(2, run.treeStage);

            run.brokenByTier[2] = 1;
            ProgressionRules.Buy(run, Raiz3);
            Assert.AreEqual(3, run.treeStage);

            run.brokenByTier[3] = 1;
            ProgressionRules.Buy(run, Despertar);
            Assert.AreEqual(4, run.treeStage);
        }

        [Test]
        public void OptionalNodeNeedsStageOneEvenWithCurrency()
        {
            var run = new RunState { wisdom = 1000 };
            Assert.AreEqual("requiere Raíz I", ProgressionRules.WhyCannotBuy(run, Entrega));
            run.treeStage = 1;
            run.SetUpgradeLevel("raiz1", 1);
            Assert.IsNull(ProgressionRules.WhyCannotBuy(run, Entrega));
            Assert.IsTrue(ProgressionRules.Buy(run, Entrega));
            Assert.IsTrue(run.HasUnlock(UnlockIds.Entrega));
        }

        [Test]
        public void BoardNodeOpensOnlyOnceItsParentReachesTheLevel()
        {
            var run = new RunState { reserveSouls = 10000 };
            var velocidad = UpgradeCatalog.Get("velocidad");
            Assert.AreEqual("requiere Energía nv 1", ProgressionRules.WhyCannotBuy(run, velocidad));
            Assert.IsFalse(ProgressionRules.Buy(run, velocidad));
            run.SetUpgradeLevel("energia", 1);
            Assert.IsNull(ProgressionRules.WhyCannotBuy(run, velocidad));
            Assert.IsTrue(ProgressionRules.Buy(run, velocidad));
        }

        [Test]
        public void EveryBoardParentExistsAndNoTwoNodesShareACell()
        {
            var cells = new System.Collections.Generic.HashSet<UnityEngine.Vector2>();
            foreach (var def in UpgradeCatalog.All)
            {
                if (!string.IsNullOrEmpty(def.requires)) Assert.IsNotNull(UpgradeCatalog.Get(def.requires), def.id + " requires an unknown node");
                Assert.IsTrue(cells.Add(def.boardPos), def.id + " overlaps another node on the board");
                Assert.AreNotEqual(UnityEngine.Vector2.zero, def.boardPos, def.id + " sits on the tree itself");
            }
        }

        [Test]
        public void LevelsSurviveJsonRoundTrip()
        {
            var run = new RunState { wisdom = 1000, reserveSouls = 1000 };
            ProgressionRules.Buy(run, Carga);
            ProgressionRules.Buy(run, Carga);
            BuyAllCoreInOrder(run);

            string json = JsonUtility.ToJson(run);
            var loaded = JsonUtility.FromJson<RunState>(json);

            Assert.AreEqual(2, loaded.UpgradeLevel("carga"));
            Assert.AreEqual(4, loaded.treeStage);
            Assert.IsTrue(loaded.HasUnlock(UnlockIds.Despertar));
            Assert.IsTrue(ProgressionRules.CoreComplete(loaded));

            var stats = new StatSheet();
            ProgressionRules.ApplyStats(loaded, stats);
            Assert.AreEqual(Balance.SpiritCapacity + Balance.CargaEffect * 2, stats.Get(StatId.SpiritCapacity), .0001f);
        }

        static void BuyAllCoreInOrder(RunState run)
        {
            run.brokenByTier[0] = 1;
            ProgressionRules.Buy(run, Raiz1);
            run.brokenByTier[1] = 1;
            ProgressionRules.Buy(run, Raiz2);
            run.brokenByTier[2] = 1;
            ProgressionRules.Buy(run, Raiz3);
            run.brokenByTier[3] = 1;
            ProgressionRules.Buy(run, Despertar);
        }
    }
}
