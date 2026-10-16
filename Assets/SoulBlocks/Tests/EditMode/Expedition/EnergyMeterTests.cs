using NUnit.Framework;

namespace SoulBlocks.Tests
{
    public class EnergyMeterTests
    {
        [Test]
        public void TickDrainsOneUnitPerSecond()
        {
            var meter = new EnergyMeter();
            meter.ResetForExpedition(25f);
            meter.Tick(1f, false);
            Assert.AreEqual(24f, meter.Current, .0001f);
        }

        [Test]
        public void PausedDoesNotDrain()
        {
            var meter = new EnergyMeter();
            meter.ResetForExpedition(25f);
            meter.Tick(5f, true);
            Assert.AreEqual(25f, meter.Current, .0001f);
        }

        [Test]
        public void WarningsFireExactlyOncePerExpedition()
        {
            var meter = new EnergyMeter();
            meter.ResetForExpedition(10f); // thresholds at 2.5 and 1.0
            int warnings = 0;
            for (int i = 0; i < 20; i++)
                if (meter.Tick(.5f, false).HasValue) warnings++;
            Assert.AreEqual(2, warnings, "expected exactly one 25% warning and one 10% warning");
        }

        [Test]
        public void ResetForExpeditionAllowsWarningsAgain()
        {
            var meter = new EnergyMeter();
            meter.ResetForExpedition(10f);
            for (int i = 0; i < 20; i++) meter.Tick(.5f, false);

            meter.ResetForExpedition(10f);
            bool warnedAgain = false;
            for (int i = 0; i < 20; i++)
                if (meter.Tick(.5f, false).HasValue) warnedAgain = true;
            Assert.IsTrue(warnedAgain, "a fresh expedition should be able to warn again");
        }

        [Test]
        public void DepletedWhenCurrentReachesZeroAndNeverGoesNegative()
        {
            var meter = new EnergyMeter();
            meter.ResetForExpedition(1f);
            meter.Tick(2f, false);
            Assert.AreEqual(0f, meter.Current, .0001f);
            Assert.IsTrue(meter.Depleted);
        }

        [Test]
        public void SyncFromClampsAndRecomputesDepleted()
        {
            var meter = new EnergyMeter();
            meter.SyncFrom(-5f, 25f);
            Assert.AreEqual(0f, meter.Current, .0001f);
            Assert.IsTrue(meter.Depleted);

            meter.SyncFrom(12f, 25f);
            Assert.AreEqual(12f, meter.Current, .0001f);
            Assert.IsFalse(meter.Depleted);
        }
    }
}
