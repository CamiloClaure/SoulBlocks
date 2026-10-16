using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SoulBlocks.Tests
{
    // One end-to-end PlayMode run through the real systems (no fakes) on the hand-built scene,
    // so a break in how the systems fit together shows up even when every system's own tests
    // are green. Split into 3 [UnityTest] methods, each booting a fresh scene. Where a shortcut
    // keeps the test short ("grant enough wisdom", a higher pickaxe level, broken-tier
    // counters) RunState is set directly instead of replaying the whole path.
    public class FullRunSmokeTests
    {
        const int Seed = 12345;
        string tempSavePath;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SaveSystem.PathOverride = null;
            if (tempSavePath != null)
            {
                if (File.Exists(tempSavePath)) File.Delete(tempSavePath);
                string bak = tempSavePath + ".bak";
                if (File.Exists(bak)) File.Delete(bak);
            }
            tempSavePath = null;
            yield return null;
        }

        // Shared boot: fixed RNG seed, a temp save path (TryBuy/GameWon call SaveSystem.SaveNow as a
        // side effect in every test below, so this keeps every test off the real save file),
        // wait for MapBuilt, dismiss the first-boot modal, stop reading the keyboard.
        IEnumerator BootFixedSeed()
        {
            tempSavePath = Path.Combine(Application.temporaryCachePath, "humo-smoke-" + Guid.NewGuid().ToString("N") + ".json");
            SaveSystem.PathOverride = tempSavePath;
            GameRoot.PendingLoad = new RunState { seed = Seed };
            StartMenu.ResetForTests(); // another test may have booted the scene first in this play session

            bool mapBuilt = false;
            Action<MapLayout> onBuilt = _ => mapBuilt = true;
            GameEvents.MapBuilt += onBuilt;
            SceneManager.LoadScene(GameRoot.SceneName);
            for (int i = 0; i < 10; i++) yield return null;
            GameEvents.MapBuilt -= onBuilt;
            Assert.IsTrue(mapBuilt, "MapBuilt was not raised on boot.");

            StartMenu.Dismiss();
            yield return null;
            GameRoot.Instance.Player.readInput = false;
        }

        [UnityTest]
        public IEnumerator SoulsFollowTheTorchToTheAltarThenTheReserveBuysTheCheapestUpgrade()
        {
            yield return BootFixedSeed();

            var root = GameRoot.Instance;
            var run = root.Run;
            var player = root.Player;
            var torch = Object.FindAnyObjectByType<GuidingTorch>();
            Assert.IsNotNull(torch, "The scene has no guiding torch.");

            // 1. Leaving the island onto a branch starts an expedition; coming back ends it.
            var tile = root.Layout.tiles.Where(t => t.branchId >= 0 && t.pathDistance >= Balance.TileSize * 1.5f && string.IsNullOrEmpty(t.requiresUnlock))
                .OrderBy(t => t.pathDistance).First();
            player.Teleport(tile.position);
            for (int i = 0; i < 10; i++) yield return null;
            Assert.IsTrue(run.inExpedition, "Leaving the hub onto a branch tile did not start an expedition.");
            player.Teleport(torch.AltarPosition + Vector3.right * 1.7f);
            for (int i = 0; i < 10; i++) yield return null;
            Assert.IsTrue(root.GetComponent<HubZone>().InHub, "The altar is not on the island.");
            Assert.IsFalse(run.inExpedition, "Returning to the island did not end the expedition.");

            // 2. Take the torch, free a few souls next to it, lead them and return it: they
            // ascend into the reserve.
            Assert.IsTrue(torch.Interact(player), "Could not take the torch from its altar.");
            GameEvents.RaiseSoulsReleased(player.transform.position + Vector3.right, 3);
            yield return new WaitForSeconds(1f);
            Assert.GreaterOrEqual(WanderingSpirit.FollowingCount, 3, "Freed souls did not follow the lit torch.");
            int reserveBefore = run.reserveSouls;
            Assert.IsTrue(torch.Interact(player), "Could not return the torch to its altar.");
            yield return new WaitForSeconds(2.5f);
            Assert.GreaterOrEqual(run.reserveSouls, reserveBefore + 3, "Guided souls did not reach the reserve.");

            // 3. Buy the cheapest Souls upgrade already open on the board with TryBuy.
            var progression = root.GetComponent<ProgressionSystem>();
            Assert.IsNotNull(progression, "ProgressionSystem was not attached.");
            var cheapest = UpgradeCatalog.Tree(UpgradeTree.Souls)
                .Where(d => ProgressionRules.RequirementMet(run, d))
                .OrderBy(d => ProgressionRules.Cost(d, run.UpgradeLevel(d.id) + 1))
                .First();
            int cost = ProgressionRules.Cost(cheapest, run.UpgradeLevel(cheapest.id) + 1);
            run.reserveSouls = Mathf.Max(run.reserveSouls, cost); // prefer earned souls, top up only the shortfall
            StatId stat = cheapest.perLevelEffects[0].stat;
            float statBefore = root.Stats.Get(stat);
            Assert.IsTrue(progression.TryBuy(cheapest.id), "TryBuy failed for the cheapest affordable Souls upgrade (" + cheapest.id + ").");
            yield return null;
            Assert.Greater(root.Stats.Get(stat), statBefore, "Buying " + cheapest.id + " did not change " + stat + ".");

            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator SwingingAtABranchEndPrisonOpensAPortalAndSurvivesASaveLoadRoundTrip()
        {
            yield return BootFixedSeed();

            var root = GameRoot.Instance;
            var run = root.Run;
            var player = root.Player;

            var soulsBranch = root.Layout.branches.FirstOrDefault(b => b.specialty == BranchSpecialty.Souls);
            Assert.IsNotNull(soulsBranch, "No Souls-specialty branch in the scene.");
            var endPrison = root.Layout.prisons.FirstOrDefault(p => p.id == soulsBranch.endPrisonId);
            Assert.IsNotNull(endPrison, "The Souls branch has no end prison.");
            run.pickaxeLevel = 1 + 10 * PrisonRules.RequiredLevel(endPrison.tier); // shortcut: the first level of the color it needs

            // 1. Stand a step before the prison, on the path, facing it: an expedition starts.
            var approach = root.Layout.tiles.Where(t => t.branchId == soulsBranch.id).OrderBy(t => (t.position - endPrison.position).sqrMagnitude).First();
            Vector3 toward = endPrison.position - approach.position;
            toward.y = 0f;
            player.Teleport(endPrison.position - toward.normalized * 1.1f);
            for (int i = 0; i < 10; i++) yield return null;
            Assert.IsTrue(run.inExpedition, "Approaching the branch-end prison did not start an expedition.");

            int wisdomBefore = run.wisdom;
            int soulsReleasedTotal = 0;
            Action<Vector3, int> onReleased = (pos, count) => soulsReleasedTotal += count;
            GameEvents.SoulsReleased += onReleased;

            // 2. Swing until it breaks (real time: batchmode frames are ~1 ms).
            var pickaxe = player.GetComponent<CrystalMiningAttack>();
            for (int guard = 0; guard < 60 && !run.Prison(endPrison.id).broken; guard++)
            {
                pickaxe.TrySwing(toward);
                yield return new WaitForSeconds(pickaxe.Duration + .05f);
            }
            GameEvents.SoulsReleased -= onReleased;

            Assert.IsTrue(run.Prison(endPrison.id).broken, "The branch-end prison never broke.");
            Assert.Greater(run.wisdom, wisdomBefore, "Run.wisdom did not increase after breaking the prison.");
            Assert.Greater(soulsReleasedTotal, 0, "No souls were released when the prison broke.");

            // Branch-end break -> blessing modal opens; pick one through RewardSystem.
            Assert.IsTrue(Keys.Blocked, "Breaking a branch-end prison should have opened the blessing modal.");
            var rewards = root.GetComponent<RewardSystem>();
            Assert.Greater(rewards.BlessingOptions.Count, 0, "No blessing options were offered.");
            Assert.IsTrue(rewards.ChooseBlessing(rewards.BlessingOptions[0]), "ChooseBlessing rejected an id from its own offer.");
            yield return null;
            Assert.IsFalse(Keys.Blocked, "Keys.Blocked stayed true after choosing a blessing.");

            // 3. Grant wisdom and buy raiz1 -> the branch portal activates and sends the player home.
            run.wisdom = 1000;
            run.brokenByTier[(int)PrisonTier.T1] = Mathf.Max(1, run.brokenByTier[(int)PrisonTier.T1]);
            var progression = root.GetComponent<ProgressionSystem>();
            Assert.IsTrue(progression.TryBuy("raiz1"), "TryBuy(raiz1) failed after granting wisdom.");
            yield return null;

            var portal = root.Layout.portals.FirstOrDefault(p => p.branchId == soulsBranch.id);
            Assert.IsNotNull(portal, "The Souls branch has no portal.");
            ExpeditionEndReason lastEndReason = default;
            bool ended = false;
            Action<ExpeditionEndReason> onEnded = r => { ended = true; lastEndReason = r; };
            GameEvents.ExpeditionEnded += onEnded;
            player.Teleport(portal.position);
            yield return new WaitForSeconds(.5f);
            GameEvents.ExpeditionEnded -= onEnded;

            Assert.IsTrue(root.GetComponent<HubZone>().InHub, "Stepping into the active portal did not return the player to the island.");
            Assert.IsTrue(ended, "ExpeditionEnded was not raised by the portal.");
            Assert.AreEqual(ExpeditionEndReason.Portal, lastEndReason, "ExpeditionEnded fired with the wrong reason.");

            // 4. Save, restart from the save: the broken prison and the progress must persist.
            int expectedWisdom = run.wisdom;
            int expectedReserve = run.reserveSouls;
            int expectedPickaxe = run.pickaxeLevel;
            SaveSystem.SaveNow();
            Assert.IsTrue(File.Exists(tempSavePath), "SaveNow did not write a file.");
            var savedRun = SaveSystem.TryLoad();
            Assert.IsNotNull(savedRun, "TryLoad returned null right after SaveNow.");

            bool mapBuiltAfterRestart = false;
            Action<MapLayout> onRebuilt = _ => mapBuiltAfterRestart = true;
            GameEvents.MapBuilt += onRebuilt;
            GameRoot.Restart(savedRun);
            for (int i = 0; i < 10; i++) yield return null;
            GameEvents.MapBuilt -= onRebuilt;
            Assert.IsTrue(mapBuiltAfterRestart, "MapBuilt was not raised after Restart.");

            var reloadedRun = GameRoot.Instance.Run;
            Assert.AreEqual(Seed, reloadedRun.seed, "The RNG seed did not survive the save/load round trip.");
            Assert.AreEqual(expectedWisdom, reloadedRun.wisdom, "Wisdom did not survive the save/load round trip.");
            Assert.AreEqual(expectedReserve, reloadedRun.reserveSouls, "Reserve souls did not survive the save/load round trip.");
            Assert.AreEqual(expectedPickaxe, reloadedRun.pickaxeLevel, "The pickaxe level did not survive the save/load round trip.");
            Assert.IsTrue(reloadedRun.HasUnlock(UnlockIds.Raiz1), "Raiz1 unlock did not survive the save/load round trip.");
            Assert.IsTrue(reloadedRun.Prison(endPrison.id).broken, "The broken prison did not survive the save/load round trip.");
            Assert.IsFalse(MineableRegistry.All.Any(t => t.PrisonId == endPrison.id), "A broken prison was respawned as a live target after loading the save.");

            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ShortcutToDespertarBreaksTheNucleoAndWins()
        {
            yield return BootFixedSeed();

            var root = GameRoot.Instance;
            var run = root.Run;

            var nucleoLayout = root.Layout.prisons.FirstOrDefault(p => p.tier == PrisonTier.Nucleo);
            Assert.IsNotNull(nucleoLayout, "The scene has no Nucleo prison.");
            Assert.AreEqual(UnlockIds.Despertar, nucleoLayout.requiresUnlock, "The Nucleo must wait for Despertar.");

            // Shortcut to the end: grant wisdom/souls, the broken-tier counters the Wisdom core
            // nodes gate on, and the red pickaxe the Nucleo needs.
            run.wisdom = 1000;
            run.reserveSouls = 5000;
            for (int tier = (int)PrisonTier.T1; tier <= (int)PrisonTier.T4; tier++) run.brokenByTier[tier] = 1;
            run.pickaxeLevel = 41;

            var progression = root.GetComponent<ProgressionSystem>();
            foreach (var id in new[] { "raiz1", "raiz2", "raiz3" })
            {
                Assert.IsTrue(progression.TryBuy(id), "TryBuy(" + id + ") failed with the gates granted directly.");
                yield return null;
            }

            bool won = false;
            Action onWon = () => won = true;
            GameEvents.GameWon += onWon;

            Assert.IsTrue(progression.TryBuy("despertar"), "TryBuy(despertar) failed with the gates granted directly.");
            yield return null;

            var nucleoTarget = MineableRegistry.All.FirstOrDefault(t => t.PrisonId == nucleoLayout.id);
            Assert.IsNotNull(nucleoTarget, "The Nucleo did not appear after buying despertar.");
            int tierLevel = PrisonRules.PickaxeTier(run.pickaxeLevel);
            for (int guard = 0; guard < 64 && !run.Prison(nucleoLayout.id).broken; guard++)
                nucleoTarget.ApplyPickHit(tierLevel);
            yield return null;
            GameEvents.GameWon -= onWon;

            Assert.IsTrue(run.Prison(nucleoLayout.id).broken, "The Nucleo never broke.");
            Assert.IsTrue(won, "GameWon was never raised.");
            Assert.IsTrue(run.victory, "Run.victory was not set.");

            var victory = root.GetComponent<VictorySystem>();
            Assert.IsTrue(victory.IsOpen, "Victory screen did not open.");
            Assert.IsTrue(Keys.Blocked, "Keys.Blocked should be true while the victory screen is open.");
            victory.Continue();
            Assert.IsFalse(victory.IsOpen, "Victory screen did not close after Continue().");
            Assert.IsFalse(Keys.Blocked, "Continue() did not restore input (Keys.Blocked still true).");
            Assert.IsFalse(root.Player.MovementLocked, "Continue() did not restore input (Player.MovementLocked still true).");

            LogAssert.NoUnexpectedReceived();
        }
    }
}
