namespace SoulBlocks
{
    // Tunables owned by og/expedicion: soul value, energy warning thresholds, where to drop the
    // player back near the tree on depletion, and how far loose souls scatter on the ground.
    public static partial class Balance
    {
        public static float SoulValue = 1f;
        public static float EnergyWarningHigh = .25f;
        public static float EnergyWarningLow = .1f;
        public static float HubReturnOffset = 2f;
        public static float GroundSoulScatterRadius = 2.5f;
    }
}
