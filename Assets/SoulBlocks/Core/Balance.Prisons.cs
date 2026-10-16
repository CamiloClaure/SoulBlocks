namespace SoulBlocks
{
    // Tunables for crystal prisons, indexed by (int)PrisonTier: T1, T2, T3, T4, Nucleo.
    public static partial class Balance
    {
        public static float[] PrisonPhases = { 1, 2, 3, 3, 3 };
        public static float[] PrisonHp = { 8, 24, 64, 140, 360 }; // blows add up: T1 is 8 picks at level 0
        public static float[] PrisonRequiredLevel = { 0, 1, 2, 3, 4 };
        public static float[] PrisonFragments = { 1, 3, 8, 20, 0 };
        public static float[] PrisonSoulsPerPhase = { 3, 5, 8, 12, 0 };

        // Pick damage per blow per level; levels above the last index clamp to it.
        public static float[] PrisonPickDamageByLevel = { 1, 2, 4, 7, 12 };

        public static float PrisonResonanceShare = .5f;
        public static float PrisonExposedCoreMultiplier = 2f;

        // A crystal's reach radius: the pick lands within PickRange of this sphere.
        public static float PrisonRadius = .8f;

        // Crystals closer than this to the player show their HP/phase label.
        public static float PrisonLabelRange = 25f;
    }
}
