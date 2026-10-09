namespace SoulBlocks
{
    // Pure gate for section 1.7 "Raiz I": a branch-end portal works once Wisdom's Raiz I is
    // unlocked and that branch's end prison has actually been broken. No RunState coupling here
    // on purpose, so the Runtime side decides how it reads "broken" (Run.Prison(id).broken).
    public static class PortalRules
    {
        public static bool IsActive(bool hasRaiz1, bool endPrisonBroken) => hasRaiz1 && endPrisonBroken;
    }
}
