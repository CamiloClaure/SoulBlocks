using NUnit.Framework;

namespace SoulBlocks.Tests
{
    public class BroteTickerTests
    {
        [Test]
        public void NoTickBeforeIntervalElapses()
        {
            var ticker = new BroteTicker();
            Assert.AreEqual(0, ticker.Tick(1f, 8f));
        }

        [Test]
        public void OneTickExactlyAtInterval()
        {
            var ticker = new BroteTicker();
            Assert.AreEqual(1, ticker.Tick(8f, 8f));
        }

        [Test]
        public void AccumulatesAcrossCallsAndCarriesRemainder()
        {
            var ticker = new BroteTicker();
            Assert.AreEqual(0, ticker.Tick(5f, 8f));
            Assert.AreEqual(1, ticker.Tick(5f, 8f)); // 10s total: one tick, 2s carried over
            Assert.AreEqual(0, ticker.Tick(5.9f, 8f)); // 2 carried + 5.9 = 7.9: still short
            Assert.AreEqual(1, ticker.Tick(.2f, 8f));  // 8.1: second tick
        }

        [Test]
        public void MultipleIntervalsInOneBigStep()
        {
            var ticker = new BroteTicker();
            Assert.AreEqual(3, ticker.Tick(25f, 8f));
        }

        [Test]
        public void ZeroOrNegativeDeltaNeverTicks()
        {
            var ticker = new BroteTicker();
            Assert.AreEqual(0, ticker.Tick(0f, 8f));
            Assert.AreEqual(0, ticker.Tick(-1f, 8f));
        }

        [Test]
        public void SproutCountGrowsWithStageAndClampsAtTheEnds()
        {
            Assert.AreEqual(Balance.BroteSproutsBase, BroteTicker.SproutCountForStage(0));
            Assert.AreEqual(Balance.BroteSproutsBase + 4, BroteTicker.SproutCountForStage(4));
            Assert.AreEqual(Balance.BroteSproutsBase + 4, BroteTicker.SproutCountForStage(9));
            Assert.AreEqual(Balance.BroteSproutsBase, BroteTicker.SproutCountForStage(-3));
        }
    }
}
