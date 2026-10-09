namespace SoulBlocks
{
    // Map generation tunables. Lengths in meters, converted to Balance.TileSize steps by MapGen.
    public static partial class Balance
    {
        public static float SoulsBranchMin = 30f;
        public static float SoulsBranchMax = 40f;
        public static float PrisonsBranchMin = 60f;
        public static float PrisonsBranchMax = 80f;
        public static float ExplorationBranchMin = 100f;
        public static float ExplorationBranchMax = 140f;
        public static float DeepExtensionLength = 60f;

        public static float TurnChance = .25f;
        public static float ShortcutChance = .35f;
        public static float GapMaxLength = 4f; // base dash is 6 m, keep gaps comfortably shorter
        public static float ShelterSpacing = 35f;
        public static float ShelterRadius = 2.5f;

        public static int SoulsBranchPrisonInterval = 4; // tiles between solo T1s on the Souls branch
        public static int SoulSpacingSouls = 2; // tiles between soul spawns, by specialty
        public static int SoulSpacingPrisons = 4;
        public static int SoulSpacingExploration = 6;
        public static int SoulBaseCountNormal = 5;
        public static int SoulBaseCountDeep = 10;

        public static float ExposedCoreChance = .25f;

        public static int MaxGenerationAttempts = 50;
        // ponytail: 40 is a stand-in so the validator has a number to check against; og/progresion
        // owns the real first-upgrade cost and may want this renamed/removed once it lands.
        public static float ValidatorFirstUpgradeCost = 40f;
    }
}
