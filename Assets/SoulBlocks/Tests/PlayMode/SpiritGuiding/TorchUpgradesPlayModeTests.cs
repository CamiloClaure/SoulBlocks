using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SoulBlocks.Tests
{
    // Carga caps how many souls follow the torch; Alcance de la antorcha widens its reach in
    // the hand.
    public class TorchUpgradesPlayModeTests
    {
        GameRoot root;
        GuidingTorch torch;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveSystem.PathOverride = Path.Combine(Application.temporaryCachePath, "torch-upgrades-save.json");
            SceneManager.LoadScene(GameRoot.SceneName);
            for (int i = 0; i < 10; i++) yield return null;
            StartMenu.Dismiss();
            root = GameRoot.Instance;
            root.Player.readInput = false;
            torch = Object.FindAnyObjectByType<GuidingTorch>();
            root.Player.Teleport(torch.AltarPosition + Vector3.right * 1.7f);
            yield return null;
            Assert.IsTrue(torch.Interact(root.Player));
        }

        [UnityTest]
        public IEnumerator SpiritCapacityCapsTheFollowers()
        {
            // Away from the altar, so the souls drifting to the docked torch drop out first.
            var tile = root.Layout.tiles.Where(t => t.branchId >= 0 && string.IsNullOrEmpty(t.requiresUnlock)).OrderByDescending(t => t.pathDistance).First();
            root.Player.Teleport(tile.position);
            yield return new WaitForSeconds(.5f);
            root.Stats.SetModifier("test", StatId.SpiritCapacity, 2f - root.Stats.Get(StatId.SpiritCapacity));
            GameEvents.RaiseSoulsReleased(root.Player.transform.position + Vector3.right, 5);
            yield return new WaitForSeconds(1f);
            Assert.AreEqual(2, WanderingSpirit.FollowingCount);
        }

        [UnityTest]
        public IEnumerator TorchRadiusSetsTheCarriedReach()
        {
            Assert.AreEqual(root.Stats.Get(StatId.TorchRadius), torch.CurrentRadius, .001f);
            root.Stats.SetModifier("test", StatId.TorchRadius, 3f);
            Assert.AreEqual(Balance.TorchRadius + 3f, torch.CurrentRadius, .001f);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SaveSystem.PathOverride = null;
            yield return null;
        }
    }
}
