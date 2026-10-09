namespace SoulBlocks
{
    // Tunables owned by og/mundo: portal geometry/cooldown and the Brote trickle.
    public static partial class Balance
    {
        public static float PortalDiameter = 2f;
        public static float PortalThickness = .2f;
        public static float PortalPadRadius = 3.5f;
        public static float PortalTriggerRadius = 1.1f;
        public static float PortalReentryCooldown = 1.5f;

        public static float BroteIntervalSeconds = 8f;
        public static int BroteSoulsPerTick = 3;
        public static float BroteToastCooldownSeconds = 30f;
        public static int BroteSproutsBase = 2;
    }
}
