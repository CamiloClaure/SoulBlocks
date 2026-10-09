namespace SoulBlocks
{
    // Tunables owned by og/azar-recompensas: extra-on-break roll (1.8), blessing stat effects
    // (1.6/1.7), risk altar cost/reward ranges (1.8). Not consts, same reasoning as every other
    // Balance file: a balance pass edits these without touching call sites.
    public static partial class Balance
    {
        // Extra al romper: weighted pick, never required for progression.
        public static float RewardWeightSouls = 50f, RewardWeightEnergy = 30f, RewardWeightSpeed = 20f;
        public static float RewardSoulsMin = 2f, RewardSoulsMax = 5f;
        public static float RewardEnergyMin = 12f, RewardEnergyMax = 24f;
        public static float RewardSpeedMultiplier = 1.15f; // lasts until ExpeditionEnded

        // Bendiciones: effects applied as "blessing.<id>" stat sources.
        public static float BlessingDashChargesAdd = 1f;
        public static float BlessingMagnetRadiusMul = 1.5f;
        public static float BlessingEnergyMaxAdd = 40f;
        public static float BlessingCargoCapacityAdd = 50f;

        // Altares de riesgo: cost/reward known before use, deterministic per altar.
        public static float AltarCostMin = 20f, AltarCostMax = 48f;
        public static float AltarRewardMin = 8f, AltarRewardMax = 20f;
        public static float AltarInteractRange = 2f;
    }
}
