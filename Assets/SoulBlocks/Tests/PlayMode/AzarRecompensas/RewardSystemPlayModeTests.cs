using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SoulBlocks.Tests
{
    public class RewardSystemPlayModeTests
    {
        [UnityTest]
        public IEnumerator BreakingABranchEndPrisonOffersABlessingAndChoosingOneAppliesAndClearsOnEnd()
        {
            bool mapBuilt = false;
            GameEvents.MapBuilt += _ => mapBuilt = true;
            string chosenBlessing = null;
            GameEvents.BlessingChosen += id => chosenBlessing = id;

            GameRoot.PendingLoad = new RunState { seed = 9001 }; // fixed seed: reproducible blessing offer
            SceneManager.LoadScene("SoulBlocks");
            for (int i = 0; i < 10; i++) yield return null;
            Assert.IsTrue(mapBuilt, "MapBuilt was not raised.");

            var root = GameRoot.Instance;
            Assert.IsNotNull(root, "GameRoot did not boot.");
            var rewards = root.GetComponent<RewardSystem>();
            Assert.IsNotNull(rewards, "RewardSystem was not attached.");

            // The player never walks off the hub in this test, so drive the expedition flag
            // directly (same shortcut other crews' PlayMode tests take for RunState setup):
            // EndExpedition below is a no-op while inExpedition is false.
            root.Run.inExpedition = true;

            var branchEnd = root.Layout.prisons.FirstOrDefault(p => p.branchEnd);
            if (branchEnd == null)
            {
                // ponytail: never assume the map shape (og/mapa owns MapGen); skip rather than
                // fail if a future layout somehow ships with no branch-end prison at all.
                Assert.Ignore("No branch-end prison in this layout.");
                yield break;
            }

            var target = MineableRegistry.All.FirstOrDefault(t => t.PrisonId == branchEnd.id);
            Assert.IsNotNull(target, "No PrisonView registered for the branch-end prison.");

            int level = PrisonRules.RequiredLevel(branchEnd.tier);
            for (int guard = 0; guard < 64 && !root.Run.Prison(branchEnd.id).broken; guard++)
                target.ApplyPickHit(level);
            Assert.IsTrue(root.Run.Prison(branchEnd.id).broken, "branch-end prison never broke.");
            yield return null;

            Assert.IsTrue(Keys.Blocked, "Keys.Blocked was not set: blessing modal did not open.");
            int expectedCount = root.Stats.GetInt(StatId.BlessingOptions);
            Assert.AreEqual(expectedCount, rewards.BlessingOptions.Count,
                "offered blessing count did not match StatId.BlessingOptions.");

            // The offer is a random N-of-4 subset (deterministic per seed, but not guaranteed to
            // include any specific id): prefer "carga" when it is there, otherwise take whatever
            // was actually offered so the test stays valid for every seed.
            string pick = rewards.BlessingOptions.Contains("carga") ? "carga" : rewards.BlessingOptions[0];
            float cargoBefore = root.Stats.Get(StatId.SpiritCapacity);

            Assert.IsTrue(rewards.ChooseBlessing(pick), "ChooseBlessing rejected an id from its own offer.");
            yield return null;

            Assert.AreEqual(pick, chosenBlessing, "GameEvents.BlessingChosen fired for a different id.");
            Assert.IsFalse(Keys.Blocked, "Keys.Blocked stayed true after choosing a blessing.");
            CollectionAssert.Contains(root.Run.activeBlessings, pick);
            if (pick == "carga")
                Assert.AreEqual(cargoBefore + Balance.BlessingSpiritCapacityAdd, root.Stats.Get(StatId.SpiritCapacity), .001f);

            ExpeditionController.Instance.EndExpedition(ExpeditionEndReason.OnFoot);
            yield return null;

            Assert.AreEqual(cargoBefore, root.Stats.Get(StatId.SpiritCapacity), .001f,
                "SpiritCapacity did not return to base after the expedition ended.");
            Assert.AreEqual(0, root.Run.activeBlessings.Count, "Run.activeBlessings was not cleared after the expedition ended.");
        }
    }
}
