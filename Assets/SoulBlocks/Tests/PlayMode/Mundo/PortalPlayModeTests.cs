using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SoulBlocks.Tests
{
    public class PortalPlayModeTests
    {
        [UnityTest]
        public IEnumerator ActivePortalSendsThePlayerBackToTheIsland()
        {
            SceneManager.LoadScene("SoulBlocks");
            for (int i = 0; i < 10; i++) yield return null;

            var root = GameRoot.Instance;
            Assert.IsNotNull(root, "GameRoot did not boot.");
            var run = root.Run;
            var player = root.Player;
            Assert.IsNotNull(player, "Player missing from the booted scene.");
            player.readInput = false;

            var branch = root.Layout.branches[0];
            Assert.GreaterOrEqual(branch.endPrisonId, 0, "Branch 0 has no end prison to break.");

            PortalLayout portal = null;
            foreach (var p in root.Layout.portals)
                if (p.branchId == branch.id) portal = p;
            Assert.IsNotNull(portal, "Branch 0 has no portal in the layout.");

            // Grant Raiz I and break the end prison, then raise both events: PortalSystem reacts
            // to either one instantly (section "## Your scope", item 1).
            run.Prison(branch.endPrisonId).broken = true;
            GameEvents.RaisePrisonBroken(branch.endPrisonId, PrisonTier.T1);
            run.AddUnlock(UnlockIds.Raiz1);
            GameEvents.RaiseUnlockGranted(UnlockIds.Raiz1);
            for (int i = 0; i < 5; i++) yield return null;

            player.Teleport(portal.position);
            for (int i = 0; i < 15; i++) yield return null;

            var hubZone = root.GetComponent<HubZone>();
            Assert.IsNotNull(hubZone, "HubZone was not attached.");
            Assert.IsTrue(hubZone.InHub, "Stepping into the active portal did not land the player back on the island.");
        }
    }
}
