using System;
using System.Collections;
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

            // The tile closest to the hub is always reachable with base energy.
            var closestTile = root.Layout.tiles[0];
            foreach (var tile in root.Layout.tiles)
                if (tile.pathDistance < closestTile.pathDistance) closestTile = tile;
            player.Teleport(closestTile.position);
            for (int i = 0; i < 10; i++) yield return null;

            Assert.IsTrue(run.inExpedition, "Expedition did not start when leaving the hub.");
            Assert.AreEqual(run.expeditionIndex, startedIndex, "ExpeditionStarted was not raised with the new index.");

            run.cargoSouls = 10;
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
            Assert.GreaterOrEqual(run.reserveSouls, 10, "Cargo was not deposited into the reserve.");
            Assert.AreEqual(0, run.cargoSouls, "Cargo was not cleared after depositing.");

            GameEvents.ExpeditionStarted -= onStarted;
            GameEvents.ExpeditionEnded -= onEnded;
        }

        [UnityTest]
        public IEnumerator SoulsReleasedSpawnsGroundSoulsThatPersistInRunState()
        {
            SceneManager.LoadScene("SoulBlocks");
            for (int i = 0; i < 10; i++) yield return null;

            var root = GameRoot.Instance;
            Assert.IsNotNull(root, "GameRoot did not boot.");
            int before = root.Run.groundSouls.Count;
            Vector3 position = root.Layout.tiles[0].position;

            GameEvents.RaiseSoulsReleased(position, 3);
            for (int i = 0; i < 5; i++) yield return null;

            Assert.AreEqual(before + 3, root.Run.groundSouls.Count, "groundSouls was not persisted.");

            int nearby = 0;
            foreach (var soul in SoulRegistry.All)
                if ((soul.Position - position).magnitude < Balance.GroundSoulScatterRadius + 1f) nearby++;
            Assert.GreaterOrEqual(nearby, 3, "New loose souls were not spawned near the release point.");
        }
    }
}
