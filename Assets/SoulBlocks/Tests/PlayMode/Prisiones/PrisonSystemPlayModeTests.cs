using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SoulBlocks.Tests
{
    public class PrisonSystemPlayModeTests
    {
        [UnityTest]
        public IEnumerator BreakingAT1RaisesEventsAndUpdatesRun()
        {
            int brokenId = -1;
            PrisonTier brokenTier = default;
            int soulBursts = 0;
            GameEvents.PrisonBroken += (id, tier) => { brokenId = id; brokenTier = tier; };
            GameEvents.SoulsReleased += (pos, count) => soulBursts++;

            SceneManager.LoadScene("SoulBlocks");
            for (int i = 0; i < 10; i++) yield return null;

            var run = GameRoot.Instance.Run;
            // Not a Resonant one: its resonance can break a neighbour too, which also raises
            // PrisonBroken (the seed is random every run, so that made this test flaky).
            var layoutPrison = GameRoot.Instance.Layout.prisons.FirstOrDefault(p => p.tier == PrisonTier.T1 && (p.props & PrisonProp.Resonant) == 0);
            if (layoutPrison == null)
            {
                // ponytail: never assume the map shape (og/mapa is replacing the stand-in);
                // skip rather than fail if a future layout happens to have no T1 at all.
                Assert.Ignore("No T1 prison in this layout.");
                yield break;
            }

            var target = MineableRegistry.All.FirstOrDefault(t => t.PrisonId == layoutPrison.id);
            Assert.IsNotNull(target, "No PrisonView registered for the T1 prison.");

            int wisdomBefore = run.wisdom;
            for (int guard = 0; guard < 64 && !run.Prison(layoutPrison.id).broken; guard++)
                target.ApplyPickHit(0);

            Assert.IsTrue(run.Prison(layoutPrison.id).broken, "T1 never broke.");
            Assert.AreEqual(layoutPrison.id, brokenId, "PrisonBroken fired for the wrong id.");
            Assert.AreEqual(PrisonTier.T1, brokenTier);
            Assert.AreEqual(1, run.brokenByTier[(int)PrisonTier.T1]);
            Assert.Greater(run.wisdom, wisdomBefore, "Run.wisdom did not increase.");
            Assert.Greater(soulBursts, 0, "SoulsReleased was never raised.");
        }
    }
}
