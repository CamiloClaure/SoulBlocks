namespace SoulBlocks
{
    // Expedition tunables: energy warning thresholds, where to drop the player back near the
    // tree on depletion, and how far souls released by a prison scatter.
    public static partial class Balance
    {
        public static float EnergyWarningHigh = .25f;
        public static float EnergyWarningLow = .1f;
        public static float HubReturnOffset = 4.5f; // from the tree center, clear of its 4 m trunk
        public static float ReleasedSoulScatterRadius = 2.5f;
    }
}
