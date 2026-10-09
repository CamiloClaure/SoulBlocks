using System.Collections.Generic;

namespace SoulBlocks
{
    // Own bookkeeping for og/azar-expedicion.
    public sealed partial class RunState
    {
        // Expedition index Run.plan was rolled for (Run.expeditionIndex + 1 at roll time).
        // Lets ExpeditionPlanningSystem skip re-rolling a plan a loaded save already has for
        // the expedition about to start.
        public int planForIndex = -1;

        // CONTRATOS.md has no "visited shelter" field: ShelterEntered/Exited are transient, so
        // the "descubrir" objective's "undiscovered shelter" needs its own persisted memory.
        // Contract request: a shared Run.visitedShelters would let other features reuse this.
        public List<int> visitedShelterIds = new List<int>();
    }
}
