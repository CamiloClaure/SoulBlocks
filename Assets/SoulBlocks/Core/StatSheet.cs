using System;
using System.Collections.Generic;

namespace SoulBlocks
{
    // Pure math: Get(stat) = (base + sum of every source's add) * (product of every source's mul).
    // Base values are snapshotted from Balance at construction, so a fresh sheet always matches
    // the current tunables even after a balance pass edits them.
    public sealed class StatSheet
    {
        struct Modifier
        {
            public float add;
            public float mul;
        }

        readonly Dictionary<StatId, float> baseValues = new Dictionary<StatId, float>();
        readonly Dictionary<StatId, Dictionary<string, Modifier>> modifiers = new Dictionary<StatId, Dictionary<string, Modifier>>();

        public event Action Changed;

        public StatSheet()
        {
            foreach (StatId stat in Enum.GetValues(typeof(StatId)))
                baseValues[stat] = BaseValue(stat);
        }

        static float BaseValue(StatId stat)
        {
            switch (stat)
            {
                case StatId.MoveSpeed: return Balance.MoveSpeed;
                case StatId.CargoCapacity: return Balance.CargoCapacity;
                case StatId.EnergyMax: return Balance.EnergyMax;
                case StatId.PickupRadius: return Balance.PickupRadius;
                case StatId.DashCharges: return Balance.DashCharges;
                case StatId.DashDistance: return Balance.DashDistance;
                case StatId.DashRechargeSeconds: return Balance.DashRechargeSeconds;
                case StatId.MagnetRadius: return Balance.MagnetRadius;
                case StatId.MagnetDuration: return Balance.MagnetDuration;
                case StatId.MagnetCooldown: return Balance.MagnetCooldown;
                case StatId.RayLevel: return Balance.RayLevel;
                case StatId.RayLength: return Balance.RayLength;
                case StatId.RayCharges: return Balance.RayCharges;
                case StatId.RaySoulsPerCharge: return Balance.RaySoulsPerCharge;
                case StatId.RayRechargeSeconds: return Balance.RayRechargeSeconds;
                case StatId.BlessingOptions: return Balance.BlessingOptions;
                default: return 0f;
            }
        }

        public float Get(StatId stat)
        {
            float add = 0f, mul = 1f;
            if (modifiers.TryGetValue(stat, out var bySource))
                foreach (var modifier in bySource.Values)
                {
                    add += modifier.add;
                    mul *= modifier.mul;
                }
            float baseValue = baseValues.TryGetValue(stat, out var b) ? b : 0f;
            return (baseValue + add) * mul;
        }

        public int GetInt(StatId stat)
        {
            float value = Get(stat);
            return (int)(value + (value >= 0f ? .5f : -.5f));
        }

        // Replaces (not stacks) this source's previous modifier for this stat.
        public void SetModifier(string source, StatId stat, float add, float mul = 1f)
        {
            if (!modifiers.TryGetValue(stat, out var bySource))
            {
                bySource = new Dictionary<string, Modifier>();
                modifiers[stat] = bySource;
            }
            bySource[source] = new Modifier { add = add, mul = mul };
            Changed?.Invoke();
        }

        public void ClearSource(string source)
        {
            bool removedAny = false;
            foreach (var bySource in modifiers.Values)
                removedAny |= bySource.Remove(source);
            if (removedAny) Changed?.Invoke();
        }
    }
}
