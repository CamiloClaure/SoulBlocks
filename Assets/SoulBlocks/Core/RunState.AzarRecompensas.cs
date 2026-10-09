using System.Collections.Generic;

namespace SoulBlocks
{
    public sealed partial class RunState
    {
        // Ids of Layout.altars already spent this expedition (Run.activeBlessings already
        // covers blessings; altars need their own list since they are spend-once-per-expedition,
        // not a persistent effect). Refreshed (cleared) by AltarSystem on ExpeditionPlanned.
        public List<int> spentAltars = new List<int>();
    }
}
