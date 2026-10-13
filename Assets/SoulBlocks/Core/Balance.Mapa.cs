namespace SoulBlocks
{
    // Map generation tunables. Lengths in meters, converted to Balance.TileSize steps by MapGen.
    public static partial class Balance
    {
        public static float SoulsBranchMin = 120f;
        public static float SoulsBranchMax = 160f;
        public static float PrisonsBranchMin = 240f;
        public static float PrisonsBranchMax = 320f;
        public static float ExplorationBranchMin = 400f;
        public static float ExplorationBranchMax = 560f;
        public static float DeepExtensionLength = 240f;

        public static float TurnChance = .25f;
        // Natural look: tile centers jitter off the 30 m grid and a Catmull-Rom ribbon of
        // varying width runs through them (PathShape). The grid stays the topology.
        public static float PathJitter = 7.5f;
        public static float PathHalfWidth = 15f;
        public static float PathWidthNoise = .2f; // half width 12..18 m, so 24..36 m wide
        // Cave shell over every ribbon (MapBuilder): rock walls along both edges, a ceiling on top.
        public static float CaveHeight = 12f;
        public static int PathSamplesPerSegment = 8;
        public static float GapMaxLength = 4f; // base dash is 6 m, keep gaps comfortably shorter
        public static float ShelterSpacing = 140f;
        public static float ShelterRadius = 13.5f; // covers most of the 30 m cave width

        // Optional side platforms: a PlatformSize square one GapMaxLength off the path edge,
        // reachable only by dash, holding PlatformSouls.
        public static float PlatformChance = .3f;
        public static int PlatformsPerBranchMax = 2;
        public static float PlatformSize = 4f;
        public static int PlatformSouls = 15;

        public static int SoulsBranchPrisonInterval = 2; // tiles (60 m) between solo T1s on the Souls branch
        public static float PrisonGroupSpacing = 6f; // meters between prisons of one pattern group
        // One soul spawn per centerline tile; souls scatter at random over the tile square.
        // Counts at full SoulDensity (StatId.SoulDensity scales them, Abundancia raises it).
        public static int SoulsPerTileSouls = 36;
        public static int SoulsPerTilePrisons = 18;
        public static int SoulsPerTileExploration = 12;
        public static int SoulsDeepMultiplier = 2;

        public static float ExposedCoreChance = .25f;

        public static int MaxGenerationAttempts = 50;
        // ponytail: 40 is a stand-in so the validator has a number to check against; og/progresion
        // owns the real first-upgrade cost and may want this renamed/removed once it lands.
        public static float ValidatorFirstUpgradeCost = 40f;
    }
}
