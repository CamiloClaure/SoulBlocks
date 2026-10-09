using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SoulBlocks;

namespace SoulBlocks.Tests.Simulacion
{
    // Fase 4 verification (plan 1.5): 200 seeds, greedy bot, no seed left blocked. Writes the
    // pacing distribution to Validation/balance.txt so the orchestrator can read it without
    // rerunning the suite. Pure C# (EconomySim has no MonoBehaviour/Physics), so this stays well
    // under the ~60 s budget even at 200 seeds.
    public class EconomySimTests
    {
        static IEnumerable<int> TwoHundredSeeds()
        {
            for (int i = 0; i < 200; i++) yield return i * 131 + 3; // same spread as MapGenRealTests, avoids consecutive-seed correlation
        }

        [Test]
        public void TwoHundredSeedsNeverBlock()
        {
            var results = TwoHundredSeeds().Select(EconomySim.Run).ToList();
            var blocked = results.Where(r => r.blocked).ToList();

            WriteReport(results);

            Assert.IsEmpty(blocked, "blocked seeds: " + string.Join(", ", blocked.Select(r => r.seed + " (after " + r.expeditions + " expeditions)")));
        }

        static void WriteReport(List<EconomySimResult> results)
        {
            var sb = new StringBuilder();
            sb.AppendLine("EconomySim balance report (" + results.Count + " seeds)");
            sb.AppendLine();

            int blockedCount = results.Count(r => r.blocked);
            sb.AppendLine("blocked seeds: " + blockedCount);
            foreach (var r in results.Where(r => r.blocked))
                sb.AppendLine("  seed " + r.seed + " after " + r.expeditions + " expeditions");
            sb.AppendLine();

            sb.AppendLine(string.Format("{0,-16}{1,8}{2,8}{3,8}{4,8}{5,8}", "milestone", "min", "median", "p90", "max", "never"));
            AppendRow(sb, "first purchase", results.Select(r => r.minutesFirstPurchase));
            AppendRow(sb, "Raiz I", results.Select(r => r.minutesRaiz1));
            AppendRow(sb, "Raiz II", results.Select(r => r.minutesRaiz2));
            AppendRow(sb, "Raiz III", results.Select(r => r.minutesRaiz3));
            AppendRow(sb, "Despertar", results.Select(r => r.minutesDespertar));
            AppendRow(sb, "Nucleo broken", results.Select(r => r.minutesNucleoBroken));

            sb.AppendLine();
            var expeditions = results.Select(r => (float)r.expeditions).OrderBy(v => v).ToList();
            sb.AppendLine("expeditions: min=" + expeditions[0] + " median=" + Percentile(expeditions, .5f) +
                " p90=" + Percentile(expeditions, .9f) + " max=" + expeditions[expeditions.Count - 1]);

            Directory.CreateDirectory("Validation");
            File.WriteAllText("Validation/balance.txt", sb.ToString());
        }

        static void AppendRow(StringBuilder sb, string label, IEnumerable<float> values)
        {
            var reached = values.Where(v => v >= 0f).OrderBy(v => v).ToList();
            int never = values.Count() - reached.Count;
            if (reached.Count == 0)
            {
                sb.AppendLine(string.Format("{0,-16}{1,8}", label, "(never reached by any seed)"));
                return;
            }
            sb.AppendLine(string.Format("{0,-16}{1,8:0.00}{2,8:0.00}{3,8:0.00}{4,8:0.00}{5,8}",
                label, reached[0], Percentile(reached, .5f), Percentile(reached, .9f), reached[reached.Count - 1], never));
        }

        static float Percentile(List<float> sorted, float p)
        {
            int idx = RoundToInt(p * (sorted.Count - 1));
            return sorted[idx];
        }

        static int RoundToInt(float v) => (int)(v + .5f);
    }
}
