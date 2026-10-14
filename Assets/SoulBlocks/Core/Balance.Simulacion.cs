namespace SoulBlocks
{
    // Tunables owned by og/simulacion: timing constants the headless greedy bot (EconomySim)
    // uses to turn a pick blow / soul pickup into simulated seconds, plus the bot's stop
    // conditions. Not gameplay-visible; a balance pass here never touches map gen or stats,
    // only how patient the economy simulation is before calling a seed blocked.
    public static partial class Balance
    {
        public static float EconHitOverheadSeconds = .15f; // per pick blow, on top of StatId.PickInterval (click reaction)
        public static float EconCollectionOverheadSeconds = .3f; // per soul-spawn stop, on top of walking there
        public static float EconSoulValueWeight = 1f; // value per soul collected, for comparing candidate branches
        public static float EconDamageValueWeight = 2f; // value per point of prison damage dealt, same comparison

        public static int EconMaxExpeditions = 500; // hard cap: still running past this = blocked
        public static int EconBlockedStaleExpeditions = 20; // consecutive no-progress expeditions = blocked
    }
}
