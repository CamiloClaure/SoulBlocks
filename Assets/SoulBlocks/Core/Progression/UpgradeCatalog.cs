using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SoulBlocks
{
    // Matches plan 1.7: Arbol de Almas (Souls currency, geometric cost, per-level stat effects)
    // and Arbol de Wisdom (Wisdom currency, flat one-off nodes, nucleo gated by broken tiers),
    // laid out as one Albion-style destiny board around the tree: Wisdom up, Recolección left,
    // Movilidad right. The pickaxe is not here: it levels up with crystal fragments at the
    // altar (CrystalMiningAttack). A node opens once its `requires` parent reaches requiresLevel.
    public static class UpgradeCatalog
    {
        public static readonly List<UpgradeDef> All = new List<UpgradeDef>
        {
            new UpgradeDef
            {
                id = "carga", tree = UpgradeTree.Souls, currency = Currency.Souls,
                name = "Carga", description = "Más almas siguen a la antorcha a la vez.",
                maxLevel = (int)Balance.CargaMaxLevel, baseCost = (int)Balance.CargaCost, growth = Balance.CargaGrowth,
                perLevelEffects = { new UpgradeEffect(StatId.SpiritCapacity, Balance.CargaEffect) },
                boardPos = new Vector2(-1.5f, 0f)
            },
            new UpgradeDef
            {
                id = "energia", tree = UpgradeTree.Souls, currency = Currency.Souls,
                name = "Energía", description = "Más tiempo fuera del árbol.",
                maxLevel = (int)Balance.EnergiaMaxLevel, baseCost = (int)Balance.EnergiaCost, growth = Balance.EnergiaGrowth,
                perLevelEffects = { new UpgradeEffect(StatId.EnergyMax, Balance.EnergiaEffect) },
                boardPos = new Vector2(1.5f, 0f)
            },
            new UpgradeDef
            {
                id = "velocidad", tree = UpgradeTree.Souls, currency = Currency.Souls,
                name = "Velocidad", description = "Caminás más rápido.",
                maxLevel = (int)Balance.VelocidadMaxLevel, baseCost = (int)Balance.VelocidadCost, growth = Balance.VelocidadGrowth,
                perLevelEffects = { new UpgradeEffect(StatId.MoveSpeed, Balance.VelocidadEffect) },
                requires = "energia", requiresLevel = 1,
                boardPos = new Vector2(3f, .6f)
            },
            new UpgradeDef
            {
                id = "radio", tree = UpgradeTree.Souls, currency = Currency.Souls,
                name = "Alcance de la antorcha", description = "La antorcha en la mano atrae almas desde más lejos.",
                maxLevel = (int)Balance.RadioMaxLevel, baseCost = (int)Balance.RadioCost, growth = Balance.RadioGrowth,
                perLevelEffects = { new UpgradeEffect(StatId.TorchRadius, Balance.RadioEffect) },
                requires = "carga", requiresLevel = 1,
                boardPos = new Vector2(-3f, .6f)
            },
            new UpgradeDef
            {
                id = "abundancia", tree = UpgradeTree.Souls, currency = Currency.Souls,
                name = "Abundancia", description = "Más almas errantes en los caminos.",
                maxLevel = (int)Balance.AbundanciaMaxLevel, baseCost = (int)Balance.AbundanciaCost, growth = Balance.AbundanciaGrowth,
                perLevelEffects = { new UpgradeEffect(StatId.SoulDensity, Balance.AbundanciaEffect) },
                requires = "carga", requiresLevel = 1,
                boardPos = new Vector2(-3f, -.6f)
            },
            new UpgradeDef
            {
                id = "dash", tree = UpgradeTree.Souls, currency = Currency.Souls,
                name = "Dash", description = "Más distancia de dash.",
                maxLevel = (int)Balance.DashMaxLevel, baseCost = (int)Balance.DashCost, growth = Balance.DashGrowth,
                perLevelEffects = { new UpgradeEffect(StatId.DashDistance, Balance.DashEffect) },
                milestoneEvery = (int)Balance.UpgradeMilestoneEvery,
                milestoneEffects = { new UpgradeEffect(StatId.DashCharges, Balance.DashMilestoneCharges) },
                milestoneText = "2da carga de dash",
                requires = "energia", requiresLevel = 1,
                boardPos = new Vector2(3f, -.6f)
            },

            new UpgradeDef
            {
                id = "raiz1", tree = UpgradeTree.Wisdom, currency = Currency.Wisdom, core = true,
                name = "Raíz I", description = "Abre portales en los extremos de rama rotos.",
                maxLevel = 1, baseCost = (int)Balance.Raiz1Cost,
                gateMinTierBroken = 0, unlockId = UnlockIds.Raiz1, setsTreeStage = 1,
                boardPos = new Vector2(0f, 1.5f)
            },
            new UpgradeDef
            {
                id = "raiz2", tree = UpgradeTree.Wisdom, currency = Currency.Wisdom, core = true,
                name = "Raíz II", description = "Abre puentes entre extremos de ramas.",
                maxLevel = 1, baseCost = (int)Balance.Raiz2Cost,
                gateMinTierBroken = 1, gateMinTreeStage = 1, unlockId = UnlockIds.Raiz2, setsTreeStage = 2,
                requires = "raiz1", requiresLevel = 1,
                boardPos = new Vector2(0f, 3f)
            },
            new UpgradeDef
            {
                id = "raiz3", tree = UpgradeTree.Wisdom, currency = Currency.Wisdom, core = true,
                name = "Raíz III", description = "Abre segmentos profundos con T4.",
                maxLevel = 1, baseCost = (int)Balance.Raiz3Cost,
                gateMinTierBroken = 2, gateMinTreeStage = 2, unlockId = UnlockIds.Raiz3, setsTreeStage = 3,
                requires = "raiz2", requiresLevel = 1,
                boardPos = new Vector2(0f, 4.5f)
            },
            new UpgradeDef
            {
                id = "despertar", tree = UpgradeTree.Wisdom, currency = Currency.Wisdom, core = true,
                name = "Despertar", description = "Abre la expedición final.",
                maxLevel = 1, baseCost = (int)Balance.DespertarCost,
                gateMinTierBroken = 3, gateMinTreeStage = 3, unlockId = UnlockIds.Despertar, setsTreeStage = 4,
                requires = "raiz3", requiresLevel = 1,
                boardPos = new Vector2(0f, 6f)
            },
            new UpgradeDef
            {
                id = "entrega", tree = UpgradeTree.Wisdom, currency = Currency.Wisdom,
                name = "Entrega en refugios", description = "Depositar carga en refugios sin volver.",
                maxLevel = 1, baseCost = (int)Balance.EntregaCost,
                gateMinTreeStage = 1, unlockId = UnlockIds.Entrega,
                requires = "raiz1", requiresLevel = 1,
                boardPos = new Vector2(-1.4f, 1.5f)
            },
            new UpgradeDef
            {
                id = "brote", tree = UpgradeTree.Wisdom, currency = Currency.Wisdom,
                name = "Brote", description = "Las almas reaparecen cerca del árbol y se recogen solas.",
                maxLevel = 1, baseCost = (int)Balance.BroteCost,
                gateMinTreeStage = 1, unlockId = UnlockIds.Brote,
                requires = "raiz1", requiresLevel = 1,
                boardPos = new Vector2(-1.4f, 2.6f)
            },
            new UpgradeDef
            {
                id = "revelacion", tree = UpgradeTree.Wisdom, currency = Currency.Wisdom,
                name = "Revelación", description = "El mapa muestra densidad, anomalías y propiedades de prisiones.",
                maxLevel = 1, baseCost = (int)Balance.RevelacionCost,
                gateMinTreeStage = 1, unlockId = UnlockIds.Revelacion,
                requires = "raiz1", requiresLevel = 1,
                boardPos = new Vector2(1.4f, 1.5f)
            },
            new UpgradeDef
            {
                id = "cadena", tree = UpgradeTree.Wisdom, currency = Currency.Wisdom,
                name = "Golpe en cadena", description = "Al romper un cristal, el golpe salta al cristal más cercano.",
                maxLevel = 1, baseCost = (int)Balance.CadenaCost,
                gateMinTreeStage = 1, unlockId = UnlockIds.Cadena,
                requires = "raiz1", requiresLevel = 1,
                boardPos = new Vector2(1.4f, 2.6f)
            },
            new UpgradeDef
            {
                id = "bendicion_extra", tree = UpgradeTree.Wisdom, currency = Currency.Wisdom,
                name = "Bendición extra", description = "4 opciones de bendición en vez de 3.",
                maxLevel = 1, baseCost = (int)Balance.BendicionExtraCost,
                gateMinTreeStage = 1, unlockId = UnlockIds.BendicionExtra,
                perLevelEffects = { new UpgradeEffect(StatId.BlessingOptions, Balance.BendicionExtraEffect) },
                requires = "raiz1", requiresLevel = 1,
                boardPos = new Vector2(-1.4f, 3.7f)
            }
        };

        static readonly Dictionary<string, UpgradeDef> byId = All.ToDictionary(d => d.id);

        public static UpgradeDef Get(string id) => byId.TryGetValue(id, out var def) ? def : null;

        public static IEnumerable<UpgradeDef> Tree(UpgradeTree tree) => All.Where(d => d.tree == tree);
    }
}
