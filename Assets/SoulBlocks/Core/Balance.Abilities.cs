namespace SoulBlocks
{
    // Ability tunables: dash, and the pickaxe chain (Golpe en cadena). Not consts, same reasoning as
    // Balance.Core.cs: a balance pass edits these without touching call sites.
    public static partial class Balance
    {
        public static float DashTravelTime = .12f;
        public static float DashObstacleRadius = .3f;
        public static float HabilidadesSampleStep = .25f;
        public static float ChainRange = 18f;
    }
}
