using UnityEngine;

namespace SoulBlocks
{
    // What a single hit (or resonance echo) did to a prison: used by both ApplyHit and the
    // tests that simulate it, and by PrisonView to raise events / pay out wisdom and souls.
    public struct HitResult
    {
        public int damage;
        public int phasesCompleted;
        public bool broken;
        public int fragments;
        public int souls;
    }

    // Pure tier/phase/HP math for crystal prisons (section 1.5 of the plan). No MonoBehaviour,
    // no Physics: PrisonView is a thin IMineable wrapper around this, PrisonRecord is the only
    // mutable state and it lives in RunState.prisons so damage persists across expeditions.
    public static class PrisonRules
    {
        public static int TotalHp(PrisonTier tier) => (int)Balance.PrisonHp[(int)tier];
        public static int Phases(PrisonTier tier) => (int)Balance.PrisonPhases[(int)tier];
        public static int RequiredLevel(PrisonTier tier) => (int)Balance.PrisonRequiredLevel[(int)tier];
        public static int Fragments(PrisonTier tier) => (int)Balance.PrisonFragments[(int)tier];
        public static int SoulsPerPhase(PrisonTier tier) => (int)Balance.PrisonSoulsPerPhase[(int)tier];

        // The pickaxe's color stage (levels 1-10 yellow = 0 ... 41-50 red = 4) is its level
        // against prisons: tier T(n+1) needs stage n, the Nucleo the red stage.
        public static int PickaxeTier(int pickaxeLevel) => (Mathf.Clamp(pickaxeLevel, 1, 50) - 1) / 10;

        static readonly string[] PickaxeTierNames = { "amarillo", "azul", "verde", "celeste", "rojo" };
        public static string PickaxeTierName(int tier) => PickaxeTierNames[Mathf.Clamp(tier, 0, PickaxeTierNames.Length - 1)];

        // Phase sizes split the total HP as evenly as possible; any remainder lands one-each on
        // the first phases so sizes never differ by more than 1 HP and always sum to TotalHp.
        static int PhaseSize(PrisonTier tier, int phaseIndex0)
        {
            int total = TotalHp(tier), phases = Phases(tier);
            int baseSize = total / phases, remainder = total % phases;
            return baseSize + (phaseIndex0 < remainder ? 1 : 0);
        }

        static int PhasesDoneAt(PrisonTier tier, int damage)
        {
            int phases = Phases(tier), cumulative = 0, done = 0;
            for (int i = 0; i < phases; i++)
            {
                cumulative += PhaseSize(tier, i);
                if (damage >= cumulative) done = i + 1;
                else break;
            }
            return done;
        }

        // Fragments total is split the same way, remainder on the last phase: breaking a prison
        // always pays out exactly the table's total, partial phases pay the rest along the way.
        static int FragmentsForPhase(PrisonTier tier, int phaseIndex0)
        {
            int total = Fragments(tier), phases = Phases(tier);
            int baseShare = total / phases;
            return phaseIndex0 == phases - 1 ? total - baseShare * (phases - 1) : baseShare;
        }

        static int PickDamage(int pickLevel)
        {
            var table = Balance.PrisonPickDamageByLevel;
            int level = Mathf.Clamp(pickLevel, 0, table.Length - 1);
            return (int)table[level];
        }

        // ExposedCore only kicks in once the prison has already taken damage before this hit,
        // never on the very first one. damageMultiplier is the external hook (anomalies etc).
        static int HitDamage(PrisonTier tier, PrisonProp props, int pickLevel, float damageMultiplier, int damageBeforeHit)
        {
            float raw = PickDamage(pickLevel);
            if ((props & PrisonProp.ExposedCore) != 0 && damageBeforeHit > 0) raw *= Balance.PrisonExposedCoreMultiplier;
            raw *= damageMultiplier;
            // ponytail: a hit that clears the level gate always deals at least 1, so a fractional
            // external multiplier can never stall a prison forever.
            return Mathf.Max(1, Mathf.RoundToInt(raw));
        }

        // The aiming preview: Broken / RequiresLevel / Breaks (one more hit finishes it) / Hits n.
        public static PrisonForecast Forecast(PrisonTier tier, PrisonRecord record, PrisonProp props, int pickLevel, float damageMultiplier = 1f)
        {
            if (record.broken) return new PrisonForecast { Kind = ForecastKind.Broken };
            int required = RequiredLevel(tier);
            if (pickLevel < required) return new PrisonForecast { Kind = ForecastKind.RequiresLevel, requiredLevel = required };

            int total = TotalHp(tier), damage = record.damage, hits = 0;
            for (int guard = 0; guard < 64 && damage < total; guard++)
            {
                damage += HitDamage(tier, props, pickLevel, damageMultiplier, damage);
                hits++;
            }
            return hits <= 1
                ? new PrisonForecast { Kind = ForecastKind.Breaks }
                : new PrisonForecast { Kind = ForecastKind.Hits, hits = hits };
        }

        // Mutates record, returns what happened. Below the required level: 0 damage, no mutation.
        public static HitResult ApplyHit(PrisonTier tier, PrisonRecord record, PrisonProp props, int pickLevel, float damageMultiplier = 1f)
        {
            if (record.broken) return new HitResult { broken = true };
            if (pickLevel < RequiredLevel(tier)) return new HitResult();

            int dealt = HitDamage(tier, props, pickLevel, damageMultiplier, record.damage);
            return ApplyDamage(tier, record, dealt);
        }

        // Flat damage, no level gate, no ExposedCore, no external multiplier: the shared landing
        // point for a direct hit's resolved number (above) and for a resonance echo (below).
        public static HitResult ApplyDamage(PrisonTier tier, PrisonRecord record, int damage)
        {
            if (record.broken || damage <= 0) return new HitResult();

            int total = TotalHp(tier);
            int remaining = total - record.damage;
            int actual = Mathf.Min(damage, remaining);
            var result = new HitResult { damage = actual };

            int before = PhasesDoneAt(tier, record.damage);
            record.damage += actual;
            int after = PhasesDoneAt(tier, record.damage);

            for (int phase = before; phase < after; phase++)
            {
                result.fragments += FragmentsForPhase(tier, phase);
                result.souls += SoulsPerPhase(tier);
            }
            result.phasesCompleted = after - before;
            record.phasesDone = after;

            if (record.damage >= total)
            {
                record.broken = true;
                result.broken = true;
            }
            return result;
        }

        // Resonance (section 1.5): floor(damage * share), at least 1 whenever there was damage
        // to echo. PrisonSystem looks up the group and applies this once per other member; it
        // never calls this again on the result, so there is no further propagation.
        public static int ResonanceDamage(int directDamage)
        {
            if (directDamage <= 0) return 0;
            return Mathf.Max(1, Mathf.FloorToInt(directDamage * Balance.PrisonResonanceShare));
        }
    }
}
