namespace SoulBlocks
{
    // Persisted state of the special blocks and the pickaxe: block and clothing levels, the
    // selected block, crystal fragments collected and the pickaxe level (1-50).
    public sealed partial class RunState
    {
        public int[] specialBlockLevels = { 1, 1, 1, 1, 1 };
        public int specialBlockClothingLevel;
        public int selectedSpecialBlock;
        public int collectedCrystals;
        public int pickaxeLevel = 1;
    }
}
