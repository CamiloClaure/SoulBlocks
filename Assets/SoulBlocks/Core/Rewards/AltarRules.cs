namespace SoulBlocks
{
    public struct AltarRoll
    {
        public int cost;
        public int reward;
    }

    // Pure cost/reward for a risk altar (plan 1.8 "Altares de riesgo"): deterministic per
    // (seed, altar id), known before the player ever gets close, never re-rolled, so there is
    // no hidden randomness on the way in or out.
    public static class AltarRules
    {
        public static AltarRoll Roll(int effectiveSeed, int altarId)
        {
            var rng = SeededRng.Stream(effectiveSeed, "altar.risk." + altarId);
            int cost = rng.Next((int)Balance.AltarCostMin, (int)Balance.AltarCostMax + 1);
            int reward = rng.Next((int)Balance.AltarRewardMin, (int)Balance.AltarRewardMax + 1);
            return new AltarRoll { cost = cost, reward = reward };
        }
    }
}
