using NUnit.Framework;
using SoulBlocks;

namespace SoulBlocks.Tests
{
    public class ChargeMeterTests
    {
        [Test]
        public void StartsFullByDefault()
        {
            var meter = new ChargeMeter(2, 3f);
            Assert.AreEqual(2, meter.Current);
        }

        [Test]
        public void TickRegeneratesOneChargePerRechargeSeconds()
        {
            var meter = new ChargeMeter(2, 3f, 0);
            meter.Tick(2.9f);
            Assert.AreEqual(0, meter.Current);
            meter.Tick(.2f);
            Assert.AreEqual(1, meter.Current);
        }

        [Test]
        public void TickNeverExceedsMax()
        {
            var meter = new ChargeMeter(1, 1f, 1);
            meter.Tick(10f);
            Assert.AreEqual(1, meter.Current);
        }

        [Test]
        public void AddProgressGrantsChargeAfterEnoughSouls()
        {
            var meter = new ChargeMeter(2, 20f, 0);
            meter.Configure(2, 20f, 25f);
            meter.AddProgress(10f);
            Assert.AreEqual(0, meter.Current);
            meter.AddProgress(15f);
            Assert.AreEqual(1, meter.Current);
        }

        [Test]
        public void AddProgressDoesNothingWithoutSoulsPerChargeConfigured()
        {
            var meter = new ChargeMeter(2, 20f, 0);
            meter.AddProgress(1000f);
            Assert.AreEqual(0, meter.Current);
        }

        [Test]
        public void ConfigureClampsCurrentWhenMaxShrinks()
        {
            var meter = new ChargeMeter(3, 3f, 3);
            meter.Configure(1, 3f);
            Assert.AreEqual(1, meter.Current);
        }

        [Test]
        public void TryConsumeDecrementsAndFailsAtZero()
        {
            var meter = new ChargeMeter(1, 3f, 1);
            Assert.IsTrue(meter.TryConsume());
            Assert.AreEqual(0, meter.Current);
            Assert.IsFalse(meter.TryConsume());
        }
    }
}
