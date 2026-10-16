using NUnit.Framework;

namespace SoulBlocks.Tests
{
    public class RewardTableTests
    {
        [Test]
        public void SameSeedAndPrisonAlwaysRollsTheSameReward()
        {
            var a = RewardTable.Roll(12345, 7);
            var b = RewardTable.Roll(12345, 7);
            Assert.AreEqual(a.kind, b.kind);
            Assert.AreEqual(a.amount, b.amount);
        }

        [Test]
        public void DifferentPrisonIdsCanRollDifferentRewards()
        {
            // Not a hard guarantee for any single pair, but across many ids at least one must
            // differ from prison 0, otherwise the stream isn't actually varying with prisonId.
            var baseline = RewardTable.Roll(999, 0);
            bool sawDifferent = false;
            for (int id = 1; id < 50; id++)
            {
                var roll = RewardTable.Roll(999, id);
                if (roll.kind != baseline.kind || roll.amount != baseline.amount)
                { sawDifferent = true; break; }
            }
            Assert.IsTrue(sawDifferent, "every prison id rolled the exact same reward as prison 0.");
        }

        [Test]
        public void SoulsAmountStaysWithinTheBalanceRange()
        {
            for (int id = 0; id < 500; id++)
            {
                var roll = RewardTable.Roll(42, id);
                if (roll.kind != RewardKind.Souls) continue;
                Assert.GreaterOrEqual(roll.amount, (int)Balance.RewardSoulsMin);
                Assert.LessOrEqual(roll.amount, (int)Balance.RewardSoulsMax);
            }
        }

        [Test]
        public void EnergyAmountStaysWithinTheBalanceRange()
        {
            for (int id = 0; id < 500; id++)
            {
                var roll = RewardTable.Roll(42, id);
                if (roll.kind != RewardKind.Energy) continue;
                Assert.GreaterOrEqual(roll.amount, (int)Balance.RewardEnergyMin);
                Assert.LessOrEqual(roll.amount, (int)Balance.RewardEnergyMax);
            }
        }

        [Test]
        public void DistributionOverManyRollsRoughlyMatchesTheConfiguredWeights()
        {
            const int samples = 6000;
            int souls = 0, energy = 0, speed = 0;
            for (int id = 0; id < samples; id++)
            {
                switch (RewardTable.Roll(777, id).kind)
                {
                    case RewardKind.Souls:
                        souls++;
                        break;
                    case RewardKind.Energy:
                        energy++;
                        break;
                    case RewardKind.Speed:
                        speed++;
                        break;
                }
            }

            float total = Balance.RewardWeightSouls + Balance.RewardWeightEnergy + Balance.RewardWeightSpeed;
            float tolerance = .05f; // 5 points of share: plenty for 6000 samples, tight enough to catch a broken split
            Assert.AreEqual(Balance.RewardWeightSouls / total, souls / (float)samples, tolerance);
            Assert.AreEqual(Balance.RewardWeightEnergy / total, energy / (float)samples, tolerance);
            Assert.AreEqual(Balance.RewardWeightSpeed / total, speed / (float)samples, tolerance);
        }
    }
}
