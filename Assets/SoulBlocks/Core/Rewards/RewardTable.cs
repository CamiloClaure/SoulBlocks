namespace SoulBlocks
{
    public enum RewardKind { Souls, Energy, Speed }

    // What a single "extra on break" roll produced. amount is unused for Speed: strength is
    // Balance.RewardSpeedMultiplier, duration is "until the expedition ends".
    public struct RewardRoll
    {
        public RewardKind kind;
        public int amount;
    }

    // Pure weighted roll for the break bonus (plan 1.8 "Extra al romper"): base rewards are
    // already guaranteed elsewhere (prisons crew), this is only ever a small sorted extra, never
    // required for progression. One stream per prison id so replaying a seed always gives the
    // same prison the same extra, independent of every other system's draws.
    public static class RewardTable
    {
        public static RewardRoll Roll(int effectiveSeed, int prisonId)
        {
            var rng = SeededRng.Stream(effectiveSeed, "reward.extra." + prisonId);
            float total = Balance.RewardWeightSouls + Balance.RewardWeightEnergy + Balance.RewardWeightSpeed;
            float roll = rng.NextFloat() * total;

            RewardKind kind;
            if (roll < Balance.RewardWeightSouls) kind = RewardKind.Souls;
            else if (roll < Balance.RewardWeightSouls + Balance.RewardWeightEnergy) kind = RewardKind.Energy;
            else kind = RewardKind.Speed;

            int amount = 0;
            if (kind == RewardKind.Souls) amount = rng.Next((int)Balance.RewardSoulsMin, (int)Balance.RewardSoulsMax + 1);
            else if (kind == RewardKind.Energy) amount = rng.Next((int)Balance.RewardEnergyMin, (int)Balance.RewardEnergyMax + 1);

            return new RewardRoll { kind = kind, amount = amount };
        }
    }
}
