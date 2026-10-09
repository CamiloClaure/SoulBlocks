using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SoulBlocks
{
    // Pure victory-screen text, built from a RunState so it is testable without a scene.
    // No MonoBehaviour: VictorySystem (Runtime) just lays these lines out in an IMGUI window.
    public static class RunSummary
    {
        static readonly string[] TierLabel = { "T1", "T2", "T3", "T4", "Núcleo" };

        public static string FormatTime(float seconds)
        {
            int total = Mathf.Max(0, Mathf.RoundToInt(seconds));
            int h = total / 3600;
            int m = (total % 3600) / 60;
            int s = total % 60;
            return h + ":" + m.ToString("00") + ":" + s.ToString("00");
        }

        public static string BrokenByTierSummary(RunState run)
        {
            var parts = new List<string>();
            for (int i = 0; i < TierLabel.Length && i < run.brokenByTier.Length; i++)
                parts.Add(TierLabel[i] + " x" + run.brokenByTier[i]);
            return string.Join(", ", parts);
        }

        // "Optional" = Wisdom-tree nodes that are not one of the 4 nucleo gates (plan 1.7).
        public static int OptionalNodesTotal() => UpgradeCatalog.Tree(UpgradeTree.Wisdom).Count(d => !d.core);

        public static int OptionalNodesBought(RunState run) =>
            UpgradeCatalog.Tree(UpgradeTree.Wisdom).Count(d => !d.core && run.UpgradeLevel(d.id) > 0);

        public static List<string> Lines(RunState run)
        {
            return new List<string>
            {
                "Semilla: " + run.effectiveSeed,
                "Tiempo de juego: " + FormatTime(run.playTimeSeconds),
                "Expediciones: " + run.expeditionIndex,
                "Prisiones rotas: " + run.totalPrisonsBroken + " (" + BrokenByTierSummary(run) + ")",
                "Almas recolectadas: " + run.totalSoulsCollected,
                "Nodos opcionales: " + OptionalNodesBought(run) + "/" + OptionalNodesTotal()
            };
        }
    }
}
