namespace SoulBlocks
{
    // Every stat any system reads or modifies (StatSheet holds their base values).
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
