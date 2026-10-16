using NUnit.Framework;

namespace SoulBlocks.Tests
{
    public class AltarRulesTests
    {
        [Test]
        public void SameSeedAndAltarAlwaysRollsTheSameCostAndReward()
        {
            var a = AltarRules.Roll(1000, 3);
            var b = AltarRules.Roll(1000, 3);
            Assert.AreEqual(a.cost, b.cost);
            Assert.AreEqual(a.reward, b.reward);
        }

        [Test]
        public void CostAndRewardStayWithinTheConfiguredRange()
        {
            for (int id = 0; id < 500; id++)
            {
                var roll = AltarRules.Roll(2000, id);
                Assert.GreaterOrEqual(roll.cost, (int)Balance.AltarCostMin);
                Assert.LessOrEqual(roll.cost, (int)Balance.AltarCostMax);
                Assert.GreaterOrEqual(roll.reward, (int)Balance.AltarRewardMin);
                Assert.LessOrEqual(roll.reward, (int)Balance.AltarRewardMax);
            }
        }

        [Test]
        public void DifferentAltarIdsCanRollDifferentValues()
        {
            var baseline = AltarRules.Roll(3000, 0);
            bool sawDifferent = false;
            for (int id = 1; id < 50; id++)
            {
                var roll = AltarRules.Roll(3000, id);
                if (roll.cost != baseline.cost || roll.reward != baseline.reward)
                { sawDifferent = true; break; }
            }
            Assert.IsTrue(sawDifferent, "every altar id rolled the exact same cost/reward as altar 0.");
        }
    }
}
