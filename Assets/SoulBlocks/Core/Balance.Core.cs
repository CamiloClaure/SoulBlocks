namespace SoulBlocks
{
    // Tunables as public static fields, not const: a balance pass (og/simulacion) edits these
    // without touching call sites. Every later feature adds its own Balance.<Feature>.cs partial.
    public static partial class Balance
    {
        public static float TileSize = 30f; // cave width, one generator cell
        public static float TileThickness = .25f;
        public static float HubPickupTreeRange = 4f;

        public static float MoveSpeed = 6f;
        public static float CargoCapacity = 100f;
        public static float EnergyMax = 100f;
        public static float PickupRadius = 1.5f;
        public static float DashCharges = 1f;
        public static float DashDistance = 6f;
        public static float DashRechargeSeconds = 3f;
        public static float MagnetRadius = 8f;
        public static float MagnetDuration = 3f;
        public static float MagnetCooldown = 12f;
        public static float RayLevel = 0f;
        public static float RayLength = 8f;
        public static float RayCharges = 1f;
        public static float RaySoulsPerCharge = 25f;
        public static float RayRechargeSeconds = 20f;
        public static float BlessingOptions = 3f;
    }
}
