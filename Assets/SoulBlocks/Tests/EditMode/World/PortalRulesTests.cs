using NUnit.Framework;

namespace SoulBlocks.Tests
{
    public class PortalRulesTests
    {
        [Test]
        public void InactiveWithoutRaiz1EvenIfBroken()
        {
            Assert.IsFalse(PortalRules.IsActive(false, true));
        }

        [Test]
        public void InactiveWithRaiz1ButUnbroken()
        {
            Assert.IsFalse(PortalRules.IsActive(true, false));
        }

        [Test]
        public void ActiveOnlyOnceBothHold()
        {
            Assert.IsTrue(PortalRules.IsActive(true, true));
        }

        [Test]
        public void InactiveWithNeither()
        {
            Assert.IsFalse(PortalRules.IsActive(false, false));
        }
    }
}
