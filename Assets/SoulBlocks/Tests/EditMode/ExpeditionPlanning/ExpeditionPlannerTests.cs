using System.Collections.Generic;
using NUnit.Framework;

namespace SoulBlocks.Tests
{
    public class ExpeditionPlannerTests
    {
        static RunState FreshRun(int seed) => new RunState { seed = seed };

        [Test]
        public void SameSeedAndIndexGivesIdenticalPlan()
        {
            var layout = MapLayoutFixtures.Simple();
            var stats = new StatSheet();

            var planA = ExpeditionPlanner.Plan(layout, FreshRun(777), stats, 1);
            var planB = ExpeditionPlanner.Plan(layout, FreshRun(777), stats, 1);

            Assert.AreEqual(Describe(planA), Describe(planB));
        }

        [Test]
        public void DifferentIndexGivesADifferentPlan()
        {
            var layout = MapLayoutFixtures.Simple();
            var stats = new StatSheet();

            var planA = ExpeditionPlanner.Plan(layout, FreshRun(777), stats, 1);
            var planB = ExpeditionPlanner.Plan(layout, FreshRun(777), stats, 2);

            Assert.AreNotEqual(Describe(planA), Describe(planB));
        }

        [Test]
        public void OffersThreeObjectivesOfDistinctKindsOnAFreshRun()
        {
            var layout = MapLayoutFixtures.Simple();
            var stats = new StatSheet();
            var run = FreshRun(42);

            var plan = ExpeditionPlanner.Plan(layout, run, stats, 1);

            Assert.AreEqual(3, plan.offered.Count);
            var kinds = new HashSet<string>();
            foreach (var objective in plan.offered) kinds.Add(objective.kind);
            Assert.AreEqual(3, kinds.Count, "Expected recolectar/fracturar/descubrir, got: " + string.Join(",", kinds));
        }

        [Test]
        public void ObjectivesNeverTargetLockedOrOutOfTierPrisons()
        {
            var layout = MapLayoutFixtures.Simple();
            var stats = new StatSheet();

            for (int seed = 0; seed < 40; seed++)
            {
                var run = FreshRun(seed);
                for (int index = 1; index <= 3; index++)
                {
                    var plan = ExpeditionPlanner.Plan(layout, run, stats, index);
                    foreach (var objective in plan.offered)
                    {
                        if (objective.kind != "fracturar") continue;
                        PrisonLayout target = null;
                        foreach (var prison in layout.prisons)
                            if (prison.id == objective.targetId) target = prison;

                        Assert.IsNotNull(target, "fracturar targeted a prison id not in the layout.");
                        Assert.IsTrue(string.IsNullOrEmpty(target.requiresUnlock) || run.HasUnlock(target.requiresUnlock),
                            "fracturar targeted a locked prison.");
                        Assert.LessOrEqual(PrisonRules.RequiredLevel(target.tier), PrisonRules.PickaxeTier(run.pickaxeLevel),
                            "fracturar targeted a prison above the current pickaxe stage.");
                        Assert.IsFalse(run.Prison(target.id).broken, "fracturar targeted an already-broken prison.");
                    }
                }
            }
        }

        [Test]
        public void RecolectarGoalFitsWhatTheTorchCanLead()
        {
            var layout = MapLayoutFixtures.Simple();
            var stats = new StatSheet();
            int capacity = stats.GetInt(StatId.SpiritCapacity);

            for (int index = 1; index <= 5; index++)
                foreach (var objective in ExpeditionPlanner.Plan(layout, FreshRun(123), stats, index).offered)
                    if (objective.kind == "recolectar")
                    {
                        Assert.LessOrEqual(objective.goal, capacity, "one trip with a full torch must be enough");
                        Assert.GreaterOrEqual(objective.goal, (int)Balance.ObjectiveRecolectarMin);
                    }
        }

        // Cheap structural fingerprint: good enough to tell "identical" from "different" plans
        // without hand-rolling Equals on every contract type.
        static string Describe(ExpeditionPlan plan)
        {
            var text = new System.Text.StringBuilder();
            text.Append("hint=").Append(plan.hintBranch).Append(';');
            foreach (var objective in plan.offered)
                text.Append(objective.kind).Append(':').Append(objective.branchId).Append(':').Append(objective.targetId).Append(':').Append(objective.goal).Append(';');
            if (plan.anomaly != null) text.Append("anomaly=").Append(plan.anomaly.kind).Append(':').Append(plan.anomaly.position);
            return text.ToString();
        }
    }
}
