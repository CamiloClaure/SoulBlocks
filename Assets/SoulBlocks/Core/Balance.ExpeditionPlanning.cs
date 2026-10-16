namespace SoulBlocks
{
    // Expedition planning tunables: objective rewards and goals, and the anomaly effects. Same reasoning as every other Balance.<Feature>.cs: a balance
    // pass edits these without touching ExpeditionPlanner's call sites.
    public static partial class Balance
    {
        public static float ObjectiveRewardWisdom = 3f;
        public static float ObjectiveRewardSouls = 5f;
        public static float ObjectiveRecolectarFraction = .6f; // goal = souls the torch can lead at once * this
        public static float ObjectiveRecolectarMin = 3f;


        public static float AnomalyAlmasSouls = 6f;
        public static float AnomalyCorrienteRadius = 5f;
        public static float AnomalyCorrienteDashMultiplier = 2f;
        public static float AnomalyPrisionMultiplier = 2f;
        public static float AnomalyPillarHeight = 3f;
    }
}
