using System.Collections.Generic;

namespace SoulBlocks
{
    // Persisted state of the expedition planning.
    public sealed partial class RunState
    {
        // Expedition index Run.plan was rolled for (Run.expeditionIndex + 1 at roll time).
        // Lets ExpeditionPlanningSystem skip re-rolling a plan a loaded save already has for
        // the expedition about to start.
        public int planForIndex = -1;

        // ShelterEntered/Exited are transient, so the "descubrir" objective's "undiscovered
        // shelter" needs its own persisted memory.
        public List<int> visitedShelterIds = new List<int>();
    }
}
