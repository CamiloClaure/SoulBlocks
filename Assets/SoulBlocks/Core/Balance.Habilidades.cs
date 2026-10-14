namespace SoulBlocks
{
    // Tunables owned by og/habilidades: Dash, Iman, Pico. Not consts, same reasoning as
    // Balance.Core.cs: a balance pass edits these without touching call sites.
    public static partial class Balance
    {
        public static float DashTravelTime = .12f;
        public static float DashObstacleRadius = .3f;
        public static float HabilidadesSampleStep = .25f;
        public static float MagnetPullSpeed = 14f;
        public static float PickRange = 2.5f; // reach from the player to a crystal's surface (its center minus PrisonRadius)
        public static float ChainRange = 18f;
    }
}
