namespace SoulBlocks
{
    // Frozen: every stat any system reads or modifies. Add a new one only through the orchestrator.
    public enum StatId
    {
        MoveSpeed,
        SpiritCapacity, // souls that can follow the torch at once
        EnergyMax,
        TorchRadius, // reach of the carried torch
        DashCharges,
        DashDistance,
        DashRechargeSeconds,
        BlessingOptions,
        SoulDensity // path spirits per spacing (PathPopulation)
    }
}
