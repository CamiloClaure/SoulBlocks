using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SoulBlocks.Tests
{
    public class FinalFlowPlayModeTests
    {
        [UnityTest]
        public IEnumerator BreakingTheNucleoWinsAndOpensVictoryScreen()
        {
            bool won = false;
            GameEvents.GameWon += () => won = true;

            SceneManager.LoadScene("SoulBlocks");
            for (int i = 0; i < 10; i++) yield return null;

            var root = GameRoot.Instance;
            var run = root.Run;
            var nucleoLayout = root.Layout.prisons.FirstOrDefault(p => p.tier == PrisonTier.Nucleo);
            Assert.IsNotNull(nucleoLayout, "No Nucleo prison in this layout (map generator issue, not Final's).");

            run.AddUnlock(UnlockIds.Despertar);
            GameEvents.RaiseUnlockGranted(UnlockIds.Despertar);
            yield return null;

            var target = MineableRegistry.All.FirstOrDefault(t => t.PrisonId == nucleoLayout.id);
            Assert.IsNotNull(target, "Nucleo PrisonView not found in MineableRegistry.All after granting Despertar.");

            for (int guard = 0; guard < 64 && !run.Prison(nucleoLayout.id).broken; guard++)
                target.ApplyPickHit(4);
            yield return null;

            Assert.IsTrue(run.Prison(nucleoLayout.id).broken, "Nucleo never broke.");
            Assert.IsTrue(won, "GameWon was never raised.");
            Assert.IsTrue(run.victory, "Run.victory was not set.");

            var victory = root.GetComponent<VictorySystem>();
            Assert.IsNotNull(victory, "VictorySystem not attached to the Game Root.");
            Assert.IsTrue(victory.IsOpen, "Victory screen did not open.");
            Assert.IsTrue(Keys.Blocked, "Keys.Blocked should be true while the victory screen is open.");

            victory.Continue();
            Assert.IsFalse(victory.IsOpen, "Victory screen did not close after Continue().");
            Assert.IsFalse(Keys.Blocked, "Keys.Blocked should be false after Continue().");
        }
    }
}
