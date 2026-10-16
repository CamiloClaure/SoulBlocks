using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SoulBlocks.Tests
{
    public class ExpeditionPlayModeTests
    {
        [UnityTest]
        public IEnumerator LeavingHubStartsExpeditionAndEnergyDepletionEndsItBackInHub()
        {
            int startedIndex = -1;
            Action<int> onStarted = i => startedIndex = i;
            GameEvents.ExpeditionStarted += onStarted;
            bool ended = false;
            ExpeditionEndReason endedReason = default;
            Action<ExpeditionEndReason> onEnded = r => { ended = true; endedReason = r; };
            GameEvents.ExpeditionEnded += onEnded;

            SceneManager.LoadScene("SoulBlocks");
            for (int i = 0; i < 10; i++) yield return null;

            var root = GameRoot.Instance;
            Assert.IsNotNull(root, "GameRoot did not boot.");
            var run = root.Run;
            var player = root.Player;
            Assert.IsNotNull(player, "Player missing from the booted scene.");
            player.readInput = false;

            // Two tiles out along a branch: past the island's edge, reachable with base energy.
            var closestTile = root.Layout.tiles.Where(t => t.branchId >= 0 && t.pathDistance >= Balance.TileSize * 1.5f && string.IsNullOrEmpty(t.requiresUnlock))
                .OrderBy(t => t.pathDistance).First();
            player.Teleport(closestTile.position);
            for (int i = 0; i < 10; i++) yield return null;

            Assert.IsTrue(run.inExpedition, "Expedition did not start when leaving the hub.");
            Assert.AreEqual(run.expeditionIndex, startedIndex, "ExpeditionStarted was not raised with the new index.");

            run.energy = .3f;

            float elapsed = 0f;
            while (!ended && elapsed < 5f)
            {
                yield return null;
                elapsed += Time.deltaTime;
            }
            Assert.IsTrue(ended, "ExpeditionEnded was not raised within 5 seconds of energy depletion.");
            Assert.AreEqual(ExpeditionEndReason.EnergyDepleted, endedReason);

            for (int i = 0; i < 10; i++) yield return null;

            var hubZone = root.GetComponent<HubZone>();
            Assert.IsNotNull(hubZone, "HubZone was not attached.");
            Assert.IsTrue(hubZone.InHub, "Player was not back on the island after energy depletion.");

            GameEvents.ExpeditionStarted -= onStarted;
            GameEvents.ExpeditionEnded -= onEnded;
        }

        [UnityTest]
        public IEnumerator SoulsReleasedSpawnsWanderingSoulsWhereThePrisonBroke()
        {
            SceneManager.LoadScene("SoulBlocks");
            for (int i = 0; i < 10; i++) yield return null;

            var root = GameRoot.Instance;
            Assert.IsNotNull(root, "GameRoot did not boot.");
            Vector3 position = root.Layout.prisons[0].position;
            int Nearby() => WanderingSpirit.All.Count(s => Vector3.Distance(s.transform.position, position) < Balance.ReleasedSoulScatterRadius + 1f);
            int before = Nearby();

            GameEvents.RaiseSoulsReleased(position, 3);
            yield return null;

            Assert.AreEqual(before + 3, Nearby(), "Released souls did not appear around the prison.");
        }
    }
}
