using NUnit.Framework;
using SoulBlocks;

namespace SoulBlocks.Tests
{
    public class FinalRulesTests
    {
        [Test]
        public void OnlyNucleoEndsTheRun()
        {
            Assert.IsFalse(FinalRules.EndsRun(PrisonTier.T1));
            Assert.IsFalse(FinalRules.EndsRun(PrisonTier.T2));
            Assert.IsFalse(FinalRules.EndsRun(PrisonTier.T3));
            Assert.IsFalse(FinalRules.EndsRun(PrisonTier.T4));
            Assert.IsTrue(FinalRules.EndsRun(PrisonTier.Nucleo));
        }
    }
}
