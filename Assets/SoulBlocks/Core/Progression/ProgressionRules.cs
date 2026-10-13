using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace SoulBlocks
{
    // Pure rules for both upgrade trees: cost, gates, purchase, stat application and benefit text.
    // No MonoBehaviour, no events: ProgressionSystem (Runtime) wraps this and raises GameEvents.
    public static class ProgressionRules
    {
        static readonly string[] TierRoman = { "I", "II", "III", "IV" }; // index = RunState.brokenByTier index (T1..T4)
        static readonly string[] StageName = { "", "Raíz I", "Raíz II", "Raíz III", "Despertar" }; // index = treeStage

        static readonly Dictionary<StatId, string> StatLabel = new Dictionary<StatId, string>
        {
            { StatId.MoveSpeed, "m/s de velocidad" },
            { StatId.PickupRadius, "m de radio de recogida" },
            { StatId.DashDistance, "m de distancia de dash" },
            { StatId.MagnetRadius, "m de radio de imán" },
            { StatId.MagnetDuration, "s de duración de imán" },
            { StatId.PickInterval, "s entre golpes de pico" },
            { StatId.BlessingOptions, "opción(es) de bendición" }
        };

        public static int Cost(UpgradeDef def, int level) =>
            Mathf.RoundToInt(def.baseCost * Mathf.Pow(def.growth <= 0f ? 1f : def.growth, level - 1));

        // null = buyable. Checked in order: max level, order/stage gate, broken-tier gate, currency.
        public static string WhyCannotBuy(RunState run, UpgradeDef def)
        {
            int level = run.UpgradeLevel(def.id);
            if (level >= def.maxLevel) return "nivel máximo";

            if (def.gateMinTreeStage > 0 && run.treeStage < def.gateMinTreeStage)
                return "requiere " + StageName[def.gateMinTreeStage];

            if (def.gateMinTierBroken >= 0 && run.brokenByTier[def.gateMinTierBroken] < 1)
                return "requiere romper una prisión " + TierRoman[def.gateMinTierBroken];

            int cost = Cost(def, level + 1);
            if (def.currency == Currency.Souls && run.reserveSouls < cost)
                return "faltan " + (cost - run.reserveSouls) + " almas";
            if (def.currency == Currency.Wisdom && run.wisdom < cost)
                return "faltan " + (cost - run.wisdom) + " Wisdom";

            return null;
        }

        public static bool Buy(RunState run, UpgradeDef def)
        {
            if (WhyCannotBuy(run, def) != null) return false;

            int level = run.UpgradeLevel(def.id) + 1;
            int cost = Cost(def, level);
            if (def.currency == Currency.Souls) run.reserveSouls -= cost;
            else run.wisdom -= cost;

            run.SetUpgradeLevel(def.id, level);
            if (!string.IsNullOrEmpty(def.unlockId)) run.AddUnlock(def.unlockId);
            if (def.setsTreeStage > run.treeStage) run.treeStage = def.setsTreeStage;
            return true;
        }

        // One SetModifier("upgrade.<id>", stat, ...) per stat this upgrade touches, folding every
        // level's effect plus every milestone reached. Source cleared when the level is 0 (never
        // bought, or a fresh StatSheet) so a stale modifier never survives a reset.
        public static void ApplyStats(RunState run, StatSheet stats)
        {
            foreach (var def in UpgradeCatalog.All)
            {
                string source = "upgrade." + def.id;
                int level = run.UpgradeLevel(def.id);
                if (level <= 0) { stats.ClearSource(source); continue; }

                var adds = new Dictionary<StatId, float>();
                var muls = new Dictionary<StatId, float>();
                Accumulate(adds, muls, def.perLevelEffects, level);

                int milestones = def.milestoneEvery > 0 ? level / def.milestoneEvery : 0;
                if (milestones > 0) Accumulate(adds, muls, def.milestoneEffects, milestones);

                foreach (var kv in adds)
                    stats.SetModifier(source, kv.Key, kv.Value, muls[kv.Key]);
            }
        }

        static void Accumulate(Dictionary<StatId, float> adds, Dictionary<StatId, float> muls, List<UpgradeEffect> effects, int times)
        {
            foreach (var effect in effects)
            {
                adds[effect.stat] = (adds.TryGetValue(effect.stat, out var a) ? a : 0f) + effect.add * times;
                muls[effect.stat] = (muls.TryGetValue(effect.stat, out var m) ? m : 1f) * Mathf.Pow(effect.mul, times);
            }
        }

        public static bool CoreComplete(RunState run) => run.UpgradeLevel("despertar") >= 1;

        // Concrete Spanish benefit of the NEXT level (current level + 1), for the shop row.
        public static string BenefitEstimate(UpgradeDef def, RunState run, StatSheet stats)
        {
            int level = run.UpgradeLevel(def.id);
            int nextLevel = level + 1;
            if (nextLevel > def.maxLevel) return "nivel máximo";

            bool nextIsMilestone = def.milestoneEvery > 0 && def.milestoneEffects.Count > 0 &&
                nextLevel % def.milestoneEvery == 0 && !string.IsNullOrEmpty(def.milestoneText);
            if (nextIsMilestone) return def.milestoneText;

            switch (def.id)
            {
                case "energia":
                    {
                        float extra = def.perLevelEffects[0].add;
                        float range = extra * stats.Get(StatId.MoveSpeed) * .5f; // round trip: half the extra time each way
                        return "+" + Fmt(extra) + " s: +" + Fmt(range) + " m de alcance ida y vuelta";
                    }
                case "carga":
                    return "+" + Fmt(def.perLevelEffects[0].add) + " almas por salida";
                case "abundancia":
                    return "+" + Fmt(def.perLevelEffects[0].add * 100f) + "% de almas en las cuevas";
                case "pico_nivel":
                    {
                        // nv X damages up to tier index X (0 = T1 .. 3 = T4), nv 4 reaches the Nucleo.
                        string tier = nextLevel <= TierRoman.Length - 1 ? TierRoman[nextLevel] : "Núcleo";
                        return "nv " + nextLevel + ": pica cristales " + tier + ", más daño por golpe";
                    }
                default:
                    if (def.perLevelEffects.Count > 0)
                    {
                        var effect = def.perLevelEffects[0];
                        string label = StatLabel.TryGetValue(effect.stat, out var l) ? l : effect.stat.ToString();
                        return (effect.add >= 0f ? "+" : "") + Fmt(effect.add) + " " + label;
                    }
                    return def.description;
            }
        }

        static string Fmt(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
