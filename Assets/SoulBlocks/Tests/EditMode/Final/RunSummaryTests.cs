using NUnit.Framework;
using SoulBlocks;

namespace SoulBlocks.Tests
{
    public class RunSummaryTests
    {
        [Test]
        public void FormatTimeZeroIsZero()
        {
            Assert.AreEqual("0:00:00", RunSummary.FormatTime(0f));
        }

        [Test]
        public void FormatTimePadsMinutesAndSeconds()
        {
            Assert.AreEqual("0:01:05", RunSummary.FormatTime(65f));
        }

        [Test]
        public void FormatTimeCarriesHours()
        {
            Assert.AreEqual("1:01:01", RunSummary.FormatTime(3661f));
        }

        [Test]
        public void FormatTimeRoundsAndNeverGoesNegative()
        {
            Assert.AreEqual("0:00:00", RunSummary.FormatTime(-5f));
            Assert.AreEqual("0:00:01", RunSummary.FormatTime(.6f));
        }

        [Test]
        public void BrokenByTierSummaryListsAllFiveTiersInOrder()
        {
            var run = new RunState();
            run.brokenByTier[0] = 2; // T1
            run.brokenByTier[3] = 1; // T4
            run.brokenByTier[4] = 1; // Nucleo
            Assert.AreEqual("T1 x2, T2 x0, T3 x0, T4 x1, Núcleo x1", RunSummary.BrokenByTierSummary(run));
        }

        [Test]
        public void OptionalNodesTotalIsTheFiveNonCoreWisdomNodes()
        {
            Assert.AreEqual(5, RunSummary.OptionalNodesTotal());
        }

        [Test]
        public void OptionalNodesBoughtCountsOnlyPurchasedOnes()
        {
            var run = new RunState();
            Assert.AreEqual(0, RunSummary.OptionalNodesBought(run));

            run.SetUpgradeLevel("entrega", 1);
            run.SetUpgradeLevel("brote", 1);
            Assert.AreEqual(2, RunSummary.OptionalNodesBought(run));

            // Core nodes never count as optional, even if bought.
            run.SetUpgradeLevel("despertar", 1);
            Assert.AreEqual(2, RunSummary.OptionalNodesBought(run));
        }

        [Test]
        public void LinesIncludeSeedTimeAndPrisonCounts()
        {
            var run = new RunState { effectiveSeed = 42, playTimeSeconds = 65f, expeditionIndex = 3, totalSoulsCollected = 77 };
            run.brokenByTier[4] = 1;

            var lines = RunSummary.Lines(run);

            Assert.That(lines, Has.Some.Contains("Semilla: 42"));
            Assert.That(lines, Has.Some.Contains("0:01:05"));
            Assert.That(lines, Has.Some.Contains("Expediciones: 3"));
            Assert.That(lines, Has.Some.Contains("Núcleo x1"));
            Assert.That(lines, Has.Some.Contains("Almas recolectadas: 77"));
            Assert.That(lines, Has.Some.Contains("Nodos opcionales: 0/5"));
        }
    }
}
