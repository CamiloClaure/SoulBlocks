using System.Linq;
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

            // The scene's paths are the ground: the first tile sits on the Ground layer.
            Vector3 first = GameRoot.Instance.Layout.tiles[0].position;
            Assert.IsTrue(Physics.Raycast(first + Vector3.up, Vector3.down, out var ground, 3f, 1 << 8), "No ground under the first tile.");
            Assert.IsNotNull(ground.collider.GetComponentInParent<AuthoredPath>(), "The first tile is not on a drawn path.");

            var player = GameRoot.Instance.Player;
            Assert.IsNotNull(player, "Player missing from the booted scene.");
            player.readInput = false;

            // Walk out along branch 0's own curve, a meter at a time, until HubZone reports leaving.
            var hubZone = GameRoot.Instance.GetComponent<HubZone>();
            Assert.IsNotNull(hubZone, "HubZone was not attached.");
            var branch = GameRoot.Instance.Layout.branches[0];
            var path = Object.FindObjectsByType<AuthoredPath>(FindObjectsSortMode.None)
                .First(p => p.role == PathRole.Branch && (p.transform.TransformPoint(p.Evaluate(0f)) - branch.anchor).sqrMagnitude < .01f);
            player.Teleport(branch.anchor);
            for (int i = 0; i < 5; i++) yield return null;
            for (int step = 1; step <= 600 && hubZone.InHub; step++)
            {
                Vector3 target = path.transform.TransformPoint(path.Evaluate(step / 600f));
                for (int frames = 0; frames < 30; frames++)
                {
                    Vector3 toward = target - player.transform.position;
                    toward.y = 0f;
                    if (toward.magnitude < .3f) break;
                    player.TryMoveSupported(toward.normalized * Mathf.Min(.3f, toward.magnitude));
                    yield return null;
                }
            }

            Assert.IsFalse(hubZone.InHub, "Player never left the hub.");
            Assert.IsTrue(leftHub, "LeftHub was not raised.");
        }
    }
}
