using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SoulBlocks.Tests
{
    public class DashAbilityPlayModeTests
    {
        [UnityTest]
        public IEnumerator DashAlongTheBranchMovesForwardAndStaysSupported()
        {
            SceneManager.LoadScene("SoulBlocks");
            for (int i = 0; i < 10; i++) yield return null;

            var root = GameRoot.Instance;
            var player = root.Player;
            player.readInput = false;
            var dash = root.GetComponent<DashAbility>();
            Assert.IsNotNull(dash, "DashAbility was not attached.");

            var branch = root.Layout.branches[0];
            Vector3 start = branch.anchor + branch.direction * 1f; // just past the island edge, on tile 0
            player.Teleport(start);
            for (int i = 0; i < 3; i++) yield return null;

            Assert.IsTrue(dash.TryDash(branch.direction), "TryDash along the branch should succeed.");
            yield return new WaitForSeconds(.6f); // time, not frames: batchmode frames are ~1 ms

            Vector3 planar = Vector3.ProjectOnPlane(player.transform.position - start, Vector3.up);
            Assert.Greater(planar.magnitude, .5f, "Player did not move forward.");
            Assert.IsTrue(SoulPlayer.IsSupportedAt(player.transform.position), "Player ended unsupported.");
        }

        [UnityTest]
        public IEnumerator DashTowardNothingPastTheBranchEndNeverLeavesTheSupport()
        {
            SceneManager.LoadScene("SoulBlocks");
            for (int i = 0; i < 10; i++) yield return null;

            var root = GameRoot.Instance;
            var player = root.Player;
            player.readInput = false;
            var dash = root.GetComponent<DashAbility>();

            // Last two tiles of branch 0 that are always present:
            // dashing further along their direction runs off the end into the void.
            TileLayout last = null, previous = null;
            foreach (var tile in root.Layout.tiles)
            {
                if (tile.branchId != 0 || !string.IsNullOrEmpty(tile.requiresUnlock)) continue;
                if (last == null || tile.pathDistance > last.pathDistance)
                { previous = last; last = tile; }
                else if (previous == null || tile.pathDistance > previous.pathDistance) previous = tile;
            }
            Assert.IsNotNull(previous, "Branch 0 needs at least two required tiles.");
            Vector3 lastTile = last.position;
            Vector3 outward = last.position - previous.position;
            player.Teleport(lastTile);
            for (int i = 0; i < 3; i++) yield return null;

            dash.TryDash(outward);
            yield return new WaitForSeconds(.6f); // time, not frames: batchmode frames are ~1 ms

            Assert.IsTrue(SoulPlayer.IsSupportedAt(player.transform.position), "Player ended unsupported past the branch end.");
        }
    }
}
