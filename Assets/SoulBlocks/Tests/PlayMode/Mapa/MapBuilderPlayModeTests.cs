using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SoulBlocks.Tests.Mapa
{
    public class MapBuilderPlayModeTests
    {
        [UnityTest]
        public IEnumerator ShelterRaisesEnteredAndExitedOnDistanceCrossing()
        {
            SceneManager.LoadScene("SoulBlocks");
            for (int i = 0; i < 10; i++) yield return null;

            var root = GameRoot.Instance;
            Assert.IsNotNull(root, "GameRoot did not boot.");
            Assert.Greater(root.Layout.shelters.Count, 0, "No shelters generated.");

            var shelter = root.Layout.shelters[0];
            bool entered = false, exited = false;
            GameEvents.ShelterEntered += id => { if (id == shelter.id) entered = true; };
            GameEvents.ShelterExited += id => { if (id == shelter.id) exited = true; };

            var player = root.Player;
            player.readInput = false;
            player.Teleport(shelter.position + Vector3.up);
            for (int i = 0; i < 5; i++) yield return null;
            Assert.IsTrue(entered, "ShelterEntered was not raised.");

            player.Teleport(shelter.position + Vector3.up + new Vector3(shelter.radius + 5f, 0, 0));
            for (int i = 0; i < 5; i++) yield return null;
            Assert.IsTrue(exited, "ShelterExited was not raised.");
        }

        [UnityTest]
        public IEnumerator StandingOnATileMarksItDiscovered()
        {
            SceneManager.LoadScene("SoulBlocks");
            for (int i = 0; i < 10; i++) yield return null;

            var root = GameRoot.Instance;
            Assert.IsNotNull(root, "GameRoot did not boot.");
            // Past the first tile: the first one, 30 m wide, overlaps the island and is already
            // discovered from the spawn point.
            var tile = root.Layout.tiles.First(t => string.IsNullOrEmpty(t.requiresUnlock) && !t.shortcut && t.pathDistance >= Balance.TileSize);

            bool discovered = false;
            GameEvents.TileDiscovered += idx => { if (idx == tile.index) discovered = true; };

            root.Player.readInput = false;
            root.Player.Teleport(tile.position + Vector3.up);
            for (int i = 0; i < 5; i++) yield return null;

            Assert.IsTrue(discovered, "TileDiscovered was not raised.");
            Assert.IsTrue(root.Run.discoveredTiles.Contains(tile.index));
        }
    }
}
