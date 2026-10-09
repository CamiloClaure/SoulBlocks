namespace SoulBlocks
{
    // Tunables owned by og/azar-expedicion: objective rewards/goal, soul density per expedition
    // and the anomaly effects. Same reasoning as every other Balance.<Feature>.cs: a balance
    // pass edits these without touching ExpeditionPlanner's call sites.
    public static partial class Balance
    {
        public static float ObjectiveRewardWisdom = 3f;
        public static float ObjectiveRewardSouls = 5f;
        public static float ObjectiveRecolectarFraction = .6f; // goal = available souls on the branch * this
        public static float ObjectiveRecolectarMin = 3f;

        public static float DensityActiveMultiplier = 2f; // hintBranch gets this
        public static float DensityOtherMin = .75f; // every other branch gets Range(min, max)
        public static float DensityOtherMax = 1f;
        public static float DensityMinimumTotal = 15f; // guaranteed souls across the whole map, per expedition

        public static float AnomalyAlmasSouls = 6f;
        public static float AnomalyCorrienteRadius = 5f;
        public static float AnomalyCorrienteDashMultiplier = 2f;
        public static float AnomalyPrisionMultiplier = 2f;
        public static float AnomalyPillarHeight = 3f;
    }
}
