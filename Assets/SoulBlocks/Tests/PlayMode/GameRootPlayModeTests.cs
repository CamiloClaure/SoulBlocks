using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SoulBlocks.Tests
{
    public class GameRootPlayModeTests
    {
        [UnityTest]
        public IEnumerator SceneBootsMapBuildsAndPlayerCanLeaveHub()
        {
            bool mapBuilt = false;
            bool leftHub = false;
            GameEvents.MapBuilt += _ => mapBuilt = true;
            GameEvents.LeftHub += () => leftHub = true;

            SceneManager.LoadScene("SoulBlocks");
            for (int i = 0; i < 10; i++) yield return null;

            Assert.IsNotNull(GameRoot.Instance, "GameRoot did not boot.");
            Assert.AreEqual(3, GameRoot.Instance.Layout.branches.Count);
            Assert.IsTrue(mapBuilt, "MapBuilt was not raised.");

            var mapBuilder = GameRoot.Instance.GetComponent<MapBuilder>();
            Assert.IsNotNull(mapBuilder, "MapBuilder was not attached.");
            var firstTile = mapBuilder.TileObject(GameRoot.Instance.Layout.tiles[0].index);
            Assert.IsNotNull(firstTile, "Generated tile missing.");
            Assert.AreEqual(8, firstTile.layer, "Generated tile is not on layer 8.");

            var player = GameRoot.Instance.Player;
            Assert.IsNotNull(player, "Player missing from the booted scene.");
            player.readInput = false;

            // Teleport just inside the island near the first branch's anchor, then walk out
            // along that branch until HubZone reports leaving the hub.
            var branch = GameRoot.Instance.Layout.branches[0];
            player.Teleport(branch.anchor - branch.direction * 1f);
            for (int i = 0; i < 5; i++) yield return null;

            var hubZone = GameRoot.Instance.GetComponent<HubZone>();
            Assert.IsNotNull(hubZone, "HubZone was not attached.");

            int frames = 0;
            while (hubZone.InHub && frames < 300)
            {
                player.TryMoveSupported(branch.direction * .2f);
                yield return null;
                frames++;
            }

            Assert.IsFalse(hubZone.InHub, "Player never left the hub.");
            Assert.IsTrue(leftHub, "LeftHub was not raised.");
        }
    }
}
