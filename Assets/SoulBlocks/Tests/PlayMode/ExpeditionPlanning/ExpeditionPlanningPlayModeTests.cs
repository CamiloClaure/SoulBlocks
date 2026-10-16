using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SoulBlocks.Tests
{
    public class ExpeditionPlanningPlayModeTests
    {
        [UnityTest]
        public IEnumerator PlanHasThreeObjectivesAndAnAnomalyAfterMapBuilt()
        {
            SceneManager.LoadScene("SoulBlocks");
            for (int i = 0; i < 10; i++) yield return null;

            var run = GameRoot.Instance.Run;
            Assert.IsNotNull(run.plan, "No plan was rolled on boot.");
            Assert.AreEqual(3, run.plan.offered.Count, "Expected 3 offered objectives.");
            Assert.IsNotNull(run.plan.anomaly, "No anomaly was set.");
        }

        [UnityTest]
        public IEnumerator ChoosingFracturarAndBreakingItsPrisonCompletesTheObjective()
        {
            int completedIndex = -1;
            System.Action<int> onCompleted = i => completedIndex = i;
            GameEvents.ObjectiveCompleted += onCompleted;

            SceneManager.LoadScene("SoulBlocks");
            for (int i = 0; i < 10; i++) yield return null;

            var root = GameRoot.Instance;
            var run = root.Run;

            int chosenIndex = -1;
            for (int i = 0; i < run.plan.offered.Count; i++)
                if (run.plan.offered[i].kind == "fracturar") chosenIndex = i;

            if (chosenIndex < 0)
            {
                // ponytail: never assume every roll contains a fracturar objective (section 1.8
                // can fall back to another kind); skip rather than fail if this plan has none.
                GameEvents.ObjectiveCompleted -= onCompleted;
                Assert.Ignore("No fracturar objective in this plan.");
                yield break;
            }

            run.plan.chosenObjective = chosenIndex;
            var objective = run.plan.offered[chosenIndex];

            IMineable target = null;
            foreach (var candidate in MineableRegistry.All)
                if (candidate.PrisonId == objective.targetId) target = candidate;
            Assert.IsNotNull(target, "No PrisonView registered for the objective's prison.");

            int wisdomBefore = run.wisdom;
            int pickLevel = PrisonRules.PickaxeTier(run.pickaxeLevel);
            for (int guard = 0; guard < 64 && !objective.done; guard++)
            {
                target.ApplyPickHit(pickLevel);
                yield return null;
            }

            Assert.IsTrue(objective.done, "Objective never completed.");
            Assert.AreEqual(chosenIndex, completedIndex, "ObjectiveCompleted fired for the wrong index.");
            Assert.Greater(run.wisdom, wisdomBefore, "Wisdom reward was not paid.");

            GameEvents.ObjectiveCompleted -= onCompleted;
        }
    }
}
