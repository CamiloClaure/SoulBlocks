namespace SoulBlocks
{
    // Tunables for both upgrade trees. A balance pass edits these without touching
    // UpgradeCatalog's structure. Cost(level) = baseCost * growth^(level-1), rounded.
    public static partial class Balance
    {
        public static float UpgradeMilestoneEvery = 3f; // "cada 3 niveles hay un hito perceptible" (1.7)

        // Arbol de Almas
        public static float CargaCost = 40f, CargaGrowth = 1.5f, CargaMaxLevel = 8f, CargaEffect = 4f;
        public static float EnergiaCost = 40f, EnergiaGrowth = 1.5f, EnergiaMaxLevel = 8f, EnergiaEffect = 32f;
        public static float VelocidadCost = 50f, VelocidadGrowth = 1.5f, VelocidadMaxLevel = 6f, VelocidadEffect = .48f;
        public static float RadioCost = 35f, RadioGrowth = 1.5f, RadioMaxLevel = 4f, RadioEffect = 1f;
        public static float AbundanciaCost = 50f, AbundanciaGrowth = 1.5f, AbundanciaMaxLevel = 4f, AbundanciaEffect = .125f;
        public static float DashCost = 60f, DashGrowth = 1.5f, DashMaxLevel = 4f, DashEffect = 1.5f, DashMilestoneCharges = 1f;

        // Arbol de Wisdom: nucleo (en orden)
        public static float Raiz1Cost = 3f, Raiz2Cost = 8f, Raiz3Cost = 20f, DespertarCost = 40f;

        // Arbol de Wisdom: opcionales (requieren etapa >= 1)
        public static float EntregaCost = 6f, BroteCost = 10f, RevelacionCost = 5f, CadenaCost = 12f;
        public static float BendicionExtraCost = 8f, BendicionExtraEffect = 1f;
    }
}
