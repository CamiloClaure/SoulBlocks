using System;
using NUnit.Framework;
using UnityEngine;
using SoulBlocks;

namespace SoulBlocks.Tests
{
    public class PrisonRulesTests
    {
        static readonly PrisonTier[] AllTiers = (PrisonTier[])Enum.GetValues(typeof(PrisonTier));

        [Test]
        public void AtRequiredLevelEveryPhaseTakesAtMostThreeHitsAndFragmentsMatchTheTable()
        {
            foreach (var tier in AllTiers)
            {
                var record = new PrisonRecord();
                int level = PrisonRules.RequiredLevel(tier);
                int totalFragments = 0, phasesSeen = 0, hitsThisPhase = 0;

                for (int guard = 0; guard < 200 && !record.broken; guard++)
                {
                    int before = record.phasesDone;
                    var result = PrisonRules.ApplyHit(tier, record, PrisonProp.None, level);
                    hitsThisPhase++;
                    totalFragments += result.fragments;

                    int newlyCompleted = record.phasesDone - before;
                    if (newlyCompleted > 0)
                    {
                        Assert.LessOrEqual(hitsThisPhase, 3, tier + " phase took more than 3 hits at its required level");
                        hitsThisPhase = 0;
                        phasesSeen += newlyCompleted;
                    }
                }

                Assert.IsTrue(record.broken, tier + " never broke within the guard");
                Assert.AreEqual(PrisonRules.Phases(tier), phasesSeen, tier + " phase count mismatch");
                Assert.AreEqual(PrisonRules.Fragments(tier), totalFragments, tier + " total fragments must equal the balance table");
            }
        }

        [Test]
        public void BelowRequiredLevelDealsNoDamageAndForecastsRequiresLevel()
        {
            foreach (var tier in AllTiers)
            {
                int required = PrisonRules.RequiredLevel(tier);
                if (required <= 0) continue; // T1 requires level 0, nothing below it to test

                var record = new PrisonRecord();
                var forecast = PrisonRules.Forecast(tier, record, PrisonProp.None, required - 1);
                Assert.AreEqual(ForecastKind.RequiresLevel, forecast.Kind, tier.ToString());
                Assert.AreEqual(required, forecast.requiredLevel, tier.ToString());

                var result = PrisonRules.ApplyHit(tier, record, PrisonProp.None, required - 1);
                Assert.AreEqual(0, result.damage, tier.ToString());
                Assert.AreEqual(0, record.damage, tier.ToString());
            }
        }

        [Test]
        public void ForecastHitCountMatchesSimulatingApplyHitUntilBroken()
        {
            var propsToTry = new[] { PrisonProp.None, PrisonProp.ExposedCore };
            foreach (var tier in AllTiers)
            foreach (var props in propsToTry)
            for (int level = 0; level <= 4; level++)
            {
                var forecast = PrisonRules.Forecast(tier, new PrisonRecord(), props, level);

                if (level < PrisonRules.RequiredLevel(tier))
                {
                    Assert.AreEqual(ForecastKind.RequiresLevel, forecast.Kind, Describe(tier, props, level));
                    continue;
                }

                var simRecord = new PrisonRecord();
                int hits = 0;
                for (int guard = 0; guard < 200 && !simRecord.broken; guard++)
                {
                    PrisonRules.ApplyHit(tier, simRecord, props, level);
                    hits++;
                }
                Assert.IsTrue(simRecord.broken, "never broke: " + Describe(tier, props, level));

                if (hits <= 1)
                    Assert.AreEqual(ForecastKind.Breaks, forecast.Kind, Describe(tier, props, level));
                else
                {
                    Assert.AreEqual(ForecastKind.Hits, forecast.Kind, Describe(tier, props, level));
                    Assert.AreEqual(hits, forecast.hits, Describe(tier, props, level));
                }
            }
        }

        static string Describe(PrisonTier tier, PrisonProp props, int level) => tier + "/" + props + "/lvl" + level;

        [Test]
        public void ResonanceDamageIsHalfRoundedDownWithAMinimumOfOne()
        {
            Assert.AreEqual(0, PrisonRules.ResonanceDamage(0));
            Assert.AreEqual(1, PrisonRules.ResonanceDamage(1));
            Assert.AreEqual(1, PrisonRules.ResonanceDamage(2));
            Assert.AreEqual(2, PrisonRules.ResonanceDamage(4));
            Assert.AreEqual(3, PrisonRules.ResonanceDamage(7));
        }

        [Test]
        public void ResonancePropagatesOnceAndNeverFromTheEchoItself()
        {
            // Three prisons sharing a group: the direct hit's damage echoes once to the other
            // two; PrisonRules.ApplyDamage (what the echo lands through) has no notion of
            // groups at all, so there is no code path for the echo to echo again.
            var hit = new PrisonRecord();
            var neighbourA = new PrisonRecord();
            var neighbourB = new PrisonRecord();
            int level = PrisonRules.RequiredLevel(PrisonTier.T2);

            var direct = PrisonRules.ApplyHit(PrisonTier.T2, hit, PrisonProp.Resonant, level);
            int echo = PrisonRules.ResonanceDamage(direct.damage);
            PrisonRules.ApplyDamage(PrisonTier.T2, neighbourA, echo);
            PrisonRules.ApplyDamage(PrisonTier.T2, neighbourB, echo);

            Assert.AreEqual(echo, neighbourA.damage);
            Assert.AreEqual(echo, neighbourB.damage);
            Assert.Greater(direct.damage, echo, "the direct hit must always outweigh its own echo");
        }

        [Test]
        public void PrisonRecordSurvivesRunStateJsonRoundTripMidDamage()
        {
            var run = new RunState();
            var record = run.Prison(3);
            PrisonRules.ApplyHit(PrisonTier.T3, record, PrisonProp.None, PrisonRules.RequiredLevel(PrisonTier.T3));
            Assert.Greater(record.damage, 0, "setup: the hit should have dealt damage");

            string json = JsonUtility.ToJson(run);
            var loaded = JsonUtility.FromJson<RunState>(json);
            var loadedRecord = loaded.Prison(3);

            Assert.AreEqual(record.damage, loadedRecord.damage);
            Assert.AreEqual(record.phasesDone, loadedRecord.phasesDone);
            Assert.AreEqual(record.broken, loadedRecord.broken);
        }
    }
}
