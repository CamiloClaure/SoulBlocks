namespace SoulBlocks
{
    // Tunables as public static fields, not const: a balance pass edits these without touching
    // call sites. Every later feature adds its own Balance.<Feature>.cs partial.
    public static partial class Balance
    {
        public static float TileSize = 30f; // a map tile's length along its path (discovery, map, planner)
        public static float HubPickupTreeRange = 4f;

        public static float MoveSpeed = 6f;
        public static float SpiritCapacity = 12f; // souls following the torch at once; Carga raises it
        public static float EnergyMax = 100f;
        public static float TorchRadius = 4f; // reach of the carried torch; Alcance raises it
        public static float DashCharges = 1f;
        public static float DashDistance = 6f;
        public static float DashRechargeSeconds = 3f;
        public static float BlessingOptions = 3f;
        public static float SoulDensity = .5f; // path spirits per PathPopulation spacing; Abundancia raises it
    }
}
