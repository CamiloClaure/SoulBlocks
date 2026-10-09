namespace SoulBlocks
{
    // Only the Nucleo ends the run (plan 1.10: Despertar opens the final expedition, breaking
    // the Nucleo is victory). Pure so VictorySystem's trigger is unit testable without a scene.
    public static class FinalRules
    {
        public static bool EndsRun(PrisonTier tier) => tier == PrisonTier.Nucleo;
    }
}
