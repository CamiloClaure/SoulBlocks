using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SoulBlocks.Tests.Humo
{
    // Fase 4 inspection: one end-to-end PlayMode run through the real systems (no fakes, no
    // mocked contracts) with a fixed seed, so a break in how two Fase 2/3 crews' systems fit
    // together shows up even though every crew's own unit/PlayMode tests are green.
    //
    // Split into 3 [UnityTest] methods for readability; each boots its own fresh "SoulBlocks"
    // scene with the same seed (MapGen is a pure function of seed + the island's fixed anchors,
    // so every assumption below about this layout - a Souls branch with a T1 branch-end prison,
    // exactly one Nucleo - is the SAME every boot) rather than chaining state across methods.
    // Where the plan allows a shortcut ("grant enough wisdom", "broken-tier counters as needed")
    // RunState is set directly instead of replaying the whole prior path, same as the other
    // crews' own PlayMode tests (e.g. RewardSystemPlayModeTests sets run.inExpedition directly).
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

        // Shared boot: fixed seed, a temp save path (TryBuy/GameWon call SaveSystem.SaveNow as a
        // side effect in every test below, so this keeps every test off the real save file),
        // wait for MapBuilt, dismiss the first-boot modal, stop reading the keyboard.
        IEnumerator BootFixedSeed()
        {
            tempSavePath = Path.Combine(Application.temporaryCachePath, "humo-smoke-" + Guid.NewGuid().ToString("N") + ".json");
            SaveSystem.PathOverride = tempSavePath;
            GameRoot.PendingSeed = Seed;
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
        public IEnumerator LeavingHubCollectsCargoReturnDepositsReserveThenBuysCheapestUpgrade()
        {
            yield return BootFixedSeed();

            var root = GameRoot.Instance;
            var run = root.Run;
            var player = root.Player;

            // Guaranteed by MapGen (3 anchors, 3 specialties, one each): a Souls branch always exists.
            var soulsBranch = root.Layout.branches.FirstOrDefault(b => b.specialty == BranchSpecialty.Souls);
            Assert.IsNotNull(soulsBranch, "No Souls-specialty branch in this layout.");

            TileLayout firstTile = null;
            foreach (var tile in root.Layout.tiles)
                if (tile.branchId == soulsBranch.id && (firstTile == null || tile.pathDistance < firstTile.pathDistance))
                    firstTile = tile;
            Assert.IsNotNull(firstTile, "Souls branch has no tiles.");

            // 1. Leave the hub onto a branch tile -> expedition started.
            player.Teleport(firstTile.position);
            for (int i = 0; i < 10; i++) yield return null;
            Assert.IsTrue(run.inExpedition, "Leaving the hub onto a branch tile did not start an expedition.");

            // Walk/teleport over a soul spawn position -> Run.cargoSouls grows.
            var spawn = root.Layout.soulSpawns.FirstOrDefault(s => s.branchId == soulsBranch.id && string.IsNullOrEmpty(s.requiresUnlock));
            Assert.IsNotNull(spawn, "Souls branch has no always-available soul spawn.");

            player.Teleport(spawn.position);
            for (int i = 0; i < 10; i++) yield return null;
            Assert.Greater(run.cargoSouls, 0, "Standing on a soul spawn did not grow cargoSouls.");

            // Return to the island -> reserveSouls grows, cargo 0. Anchor - direction*1 is the
            // same "just inside the island" offset GameRootPlayModeTests uses for branch 0; it
            // holds for every anchor (CONTRATOS.md: all 3 anchors are tested island-edge points).
            player.Teleport(soulsBranch.anchor - soulsBranch.direction * 1f);
            for (int i = 0; i < 10; i++) yield return null;

            var hubZone = root.GetComponent<HubZone>();
            Assert.IsNotNull(hubZone, "HubZone was not attached.");
            Assert.IsTrue(hubZone.InHub, "Teleporting back onto the island did not register as being in the hub.");
            Assert.IsFalse(run.inExpedition, "Returning to the island did not end the expedition.");
            Assert.AreEqual(0, run.cargoSouls, "Cargo was not deposited into the reserve on return.");
            Assert.Greater(run.reserveSouls, 0, "Reserve did not grow from the deposited cargo.");

            // 2. Buy the cheapest affordable Souls upgrade with TryBuy.
            var progression = root.GetComponent<ProgressionSystem>();
            Assert.IsNotNull(progression, "ProgressionSystem was not attached.");

            var cheapest = UpgradeCatalog.Tree(UpgradeTree.Souls)
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
        public IEnumerator BreakingAT1OpensAPortalAndSurvivesASaveLoadRoundTrip()
        {
            yield return BootFixedSeed();

            var root = GameRoot.Instance;
            var run = root.Run;
            var player = root.Player;

            var soulsBranch = root.Layout.branches.FirstOrDefault(b => b.specialty == BranchSpecialty.Souls);
            Assert.IsNotNull(soulsBranch, "No Souls-specialty branch in this layout.");
            Assert.GreaterOrEqual(soulsBranch.endPrisonId, 0, "Souls branch has no end prison.");

            // The Souls branch's end prison is always T1 (MapGen.BuildBranch: endTier = T1 for
            // BranchSpecialty.Souls); breaking it doubles as "break a T1" and "break a branch end".
            var endPrison = root.Layout.prisons.FirstOrDefault(p => p.id == soulsBranch.endPrisonId);
            Assert.IsNotNull(endPrison, "Souls branch endPrisonId does not resolve to a prison.");
            Assert.AreEqual(PrisonTier.T1, endPrison.tier, "Souls branch end prison is not T1.");

            // Last two required (non-deep, non-shortcut) tiles on the branch: the branch-end
            // prison sits at the same position as the last one, so firing from the second-to-last
            // keeps the shot well inside the default 8 m RayLength (same pattern as
            // DashAbilityPlayModeTests' "last two tiles" search).
            TileLayout last = null, nearEnd = null;
            foreach (var tile in root.Layout.tiles)
            {
                if (tile.branchId != soulsBranch.id || tile.shortcut || !string.IsNullOrEmpty(tile.requiresUnlock)) continue;
                if (last == null || tile.pathDistance > last.pathDistance) { nearEnd = last; last = tile; }
                else if (nearEnd == null || tile.pathDistance > nearEnd.pathDistance) nearEnd = tile;
            }
            Assert.IsNotNull(nearEnd, "Souls branch needs at least two required tiles to fire Rayhit from close range.");

            player.Teleport(nearEnd.position);
            for (int i = 0; i < 10; i++) yield return null;
            Assert.IsTrue(run.inExpedition, "Approaching the branch-end prison did not start an expedition.");

            var rayhit = root.GetComponent<RayhitAbility>();
            Assert.IsNotNull(rayhit, "RayhitAbility was not attached.");
            var target = RayTargetRegistry.All.FirstOrDefault(t => t.PrisonId == endPrison.id);
            Assert.IsNotNull(target, "No PrisonView registered for the branch-end T1 prison.");

            int wisdomBefore = run.wisdom;
            int soulsReleasedTotal = 0;
            Action<Vector3, int> onReleased = (pos, count) => soulsReleasedTotal += count;
            GameEvents.SoulsReleased += onReleased;

            // 3. Break the T1 with RayhitAbility.Fire aimed through it. T1 has 2 HP and the
            // default charge is 1, so the loop recharges via GameEvents.SoulsCollected (the
            // documented "1 charge per 25 souls" path) between shots instead of waiting the
            // real 20 s timer; ApplyRayHit is only the fallback if Fire ever refuses to fire.
            bool usedFallback = false;
            for (int guard = 0; guard < 20 && !run.Prison(endPrison.id).broken; guard++)
            {
                if (rayhit.Charges <= 0)
                {
                    GameEvents.RaiseSoulsCollected(Mathf.Max(1, root.Stats.GetInt(StatId.RaySoulsPerCharge)));
                    yield return null;
                }

                if (rayhit.Charges > 0 && rayhit.Fire(endPrison.position))
                {
                    yield return new WaitForSeconds(.6f); // time, not frames: batchmode frames are ~1 ms
                }
                else
                {
                    usedFallback = true;
                    target.ApplyRayHit(root.Stats.GetInt(StatId.RayLevel));
                    yield return null;
                }
            }
            GameEvents.SoulsReleased -= onReleased;

            Assert.IsTrue(run.Prison(endPrison.id).broken, "T1 branch-end prison never broke."
                + (usedFallback ? " (used ApplyRayHit fallback for at least one hit)" : ""));
            Assert.GreaterOrEqual(run.brokenByTier[(int)PrisonTier.T1], 1, "brokenByTier[T1] did not increase.");
            Assert.Greater(run.wisdom, wisdomBefore, "Run.wisdom did not increase after breaking the T1 prison.");
            Assert.Greater(soulsReleasedTotal, 0, "No loose souls were released when the prison broke.");

            // Branch-end break -> blessing modal opens; pick one through RewardSystem.
            Assert.IsTrue(Keys.Blocked, "Breaking a branch-end prison should have opened the blessing modal.");
            var rewards = root.GetComponent<RewardSystem>();
            Assert.IsNotNull(rewards, "RewardSystem was not attached.");
            Assert.Greater(rewards.BlessingOptions.Count, 0, "No blessing options were offered.");

            string pick = rewards.BlessingOptions.Contains("carga") ? "carga" : rewards.BlessingOptions[0];
            Assert.IsTrue(rewards.ChooseBlessing(pick), "ChooseBlessing rejected an id from its own offer.");
            yield return null;
            Assert.IsFalse(Keys.Blocked, "Keys.Blocked stayed true after choosing a blessing.");

            // Rayhit travel left the player on the branch end, which is where the portal sits:
            // step back first so the portal does not fire the instant raiz1 activates it.
            player.Teleport(nearEnd.position);
            yield return new WaitForSeconds(.2f);

            // 4. Grant enough wisdom and buy raiz1 -> portal at the broken branch end activates.
            run.wisdom = 1000; // shortcut: grant wisdom instead of farming more prisons for it
            var progression = root.GetComponent<ProgressionSystem>();
            Assert.IsNotNull(progression, "ProgressionSystem was not attached.");
            Assert.IsTrue(progression.TryBuy("raiz1"), "TryBuy(raiz1) failed after granting wisdom and breaking a T1.");
            yield return null;

            var portal = root.Layout.portals.FirstOrDefault(p => p.branchId == soulsBranch.id);
            Assert.IsNotNull(portal, "Souls branch has no portal in the layout.");

            ExpeditionEndReason lastEndReason = default;
            bool ended = false;
            Action<ExpeditionEndReason> onEnded = r => { ended = true; lastEndReason = r; };
            GameEvents.ExpeditionEnded += onEnded;

            player.Teleport(portal.position);
            Vector3 arrivedAt = player.transform.position;
            yield return new WaitForSeconds(.5f); // time, not frames: batchmode frames are ~1 ms
            GameEvents.ExpeditionEnded -= onEnded;

            var hubZone = root.GetComponent<HubZone>();
            Assert.IsNotNull(hubZone, "HubZone was not attached.");
            Assert.IsTrue(hubZone.InHub, "Stepping into the active portal did not return the player to the island."
                + " portal=" + portal.position + " arrivedAt=" + arrivedAt + " now=" + player.transform.position
                + " tree=" + (root.Tree != null ? root.Tree.position.ToString() : "null") + " ended=" + ended
                + " inExpedition=" + run.inExpedition + " supportedNow=" + SoulPlayer.IsSupportedAt(player.transform.position)
                + " ringActive=" + (GameObject.Find("Portal rama " + portal.branchId) != null)
                + " raiz1=" + run.HasUnlock(UnlockIds.Raiz1) + " branchEndPrisonId=" + soulsBranch.endPrisonId
                + " testedPrison=" + endPrison.id + " endBroken=" + run.Prison(soulsBranch.endPrisonId).broken);
            Assert.IsFalse(run.inExpedition, "The expedition was not ended by the portal.");
            Assert.IsTrue(ended, "ExpeditionEnded was not raised by the portal.");
            Assert.AreEqual(ExpeditionEndReason.Portal, lastEndReason, "ExpeditionEnded fired with the wrong reason.");

            // 5. SaveSystem.SaveNow(), then GameRoot.Restart(null, SaveSystem.TryLoad()): seed,
            // unlocks, wisdom, reserve and the broken prison must all match what was saved, and
            // the broken prison must not respawn as a live Rayhit target.
            int expectedWisdom = run.wisdom;
            int expectedReserve = run.reserveSouls;
            int expectedEffectiveSeed = run.effectiveSeed;
            int expectedBrokenT1Count = run.brokenByTier[(int)PrisonTier.T1];

            SaveSystem.SaveNow();
            Assert.IsTrue(File.Exists(tempSavePath), "SaveNow did not write a file.");
            var savedRun = SaveSystem.TryLoad();
            Assert.IsNotNull(savedRun, "TryLoad returned null right after SaveNow.");

            bool mapBuiltAfterRestart = false;
            Action<MapLayout> onRebuilt = _ => mapBuiltAfterRestart = true;
            GameEvents.MapBuilt += onRebuilt;
            GameRoot.Restart(null, savedRun);
            for (int i = 0; i < 10; i++) yield return null;
            GameEvents.MapBuilt -= onRebuilt;
            Assert.IsTrue(mapBuiltAfterRestart, "MapBuilt was not raised after Restart.");

            var reloadedRun = GameRoot.Instance.Run;
            Assert.AreEqual(Seed, reloadedRun.seed, "Seed did not survive the save/load round trip.");
            Assert.AreEqual(expectedEffectiveSeed, reloadedRun.effectiveSeed, "Effective seed did not survive the save/load round trip (MapGen should be deterministic for the same seed).");
            Assert.AreEqual(expectedWisdom, reloadedRun.wisdom, "Wisdom did not survive the save/load round trip.");
            Assert.AreEqual(expectedReserve, reloadedRun.reserveSouls, "Reserve souls did not survive the save/load round trip.");
            Assert.IsTrue(reloadedRun.HasUnlock(UnlockIds.Raiz1), "Raiz1 unlock did not survive the save/load round trip.");
            Assert.IsTrue(reloadedRun.Prison(endPrison.id).broken, "The broken prison did not survive the save/load round trip.");
            Assert.AreEqual(expectedBrokenT1Count, reloadedRun.brokenByTier[(int)PrisonTier.T1], "brokenByTier[T1] did not survive the save/load round trip.");

            bool respawnedAsLiveTarget = RayTargetRegistry.All.Any(t => t.PrisonId == endPrison.id);
            Assert.IsFalse(respawnedAsLiveTarget, "A broken prison was respawned as a live Rayhit target after loading the save.");

            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ShortcutToDespertarBreaksTheNucleoAndWins()
        {
            yield return BootFixedSeed();

            var root = GameRoot.Instance;
            var run = root.Run;

            // MapValidator.CheckTierCoverage requires exactly one Nucleo prison, every seed: a
            // missing Nucleo is a map-generator regression, not a per-seed possibility.
            var nucleoLayout = root.Layout.prisons.FirstOrDefault(p => p.tier == PrisonTier.Nucleo);
            Assert.IsNotNull(nucleoLayout, "No Nucleo prison in this layout (MapValidator guarantees exactly one).");

            // 6. Shortcut to the end: grant wisdom/souls and the broken-tier counters the Wisdom
            // nucleo nodes gate on, instead of actually farming and breaking one of each tier.
            run.wisdom = 1000;
            run.reserveSouls = 5000;
            run.brokenByTier[(int)PrisonTier.T1] = 1;
            run.brokenByTier[(int)PrisonTier.T2] = 1;
            run.brokenByTier[(int)PrisonTier.T3] = 1;
            run.brokenByTier[(int)PrisonTier.T4] = 1;

            var progression = root.GetComponent<ProgressionSystem>();
            Assert.IsNotNull(progression, "ProgressionSystem was not attached.");

            Assert.IsTrue(progression.TryBuy("raiz1"), "TryBuy(raiz1) failed with the gates granted directly.");
            yield return null;
            Assert.IsTrue(progression.TryBuy("raiz2"), "TryBuy(raiz2) failed with the gates granted directly.");
            yield return null;
            Assert.IsTrue(progression.TryBuy("raiz3"), "TryBuy(raiz3) failed with the gates granted directly.");
            yield return null;

            for (int level = 1; level <= 4; level++)
            {
                Assert.IsTrue(progression.TryBuy("rayhit_nivel"), "TryBuy(rayhit_nivel) failed at level " + level + ".");
                yield return null;
            }
            Assert.AreEqual(4, root.Stats.GetInt(StatId.RayLevel), "RayLevel is not 4 after 4 levels of rayhit_nivel.");

            bool won = false;
            Action onWon = () => won = true;
            GameEvents.GameWon += onWon;

            Assert.IsTrue(progression.TryBuy("despertar"), "TryBuy(despertar) failed with the gates granted directly.");
            yield return null;

            var nucleoTarget = RayTargetRegistry.All.FirstOrDefault(t => t.PrisonId == nucleoLayout.id);
            Assert.IsNotNull(nucleoTarget, "Nucleo did not appear in RayTargetRegistry.All after buying despertar.");

            for (int guard = 0; guard < 40 && !run.Prison(nucleoLayout.id).broken; guard++)
                nucleoTarget.ApplyRayHit(4);
            yield return null;
            GameEvents.GameWon -= onWon;

            Assert.IsTrue(run.Prison(nucleoLayout.id).broken, "Nucleo never broke.");
            Assert.IsTrue(won, "GameWon was never raised.");
            Assert.IsTrue(run.victory, "Run.victory was not set.");

            var victory = root.GetComponent<VictorySystem>();
            Assert.IsNotNull(victory, "VictorySystem was not attached.");
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
