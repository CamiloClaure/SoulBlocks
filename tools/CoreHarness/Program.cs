using System;
using System.Collections.Generic;
using System.Diagnostics;
using SoulBlocks;
using UnityEngine;

// Runs the seeded map generator + validator outside Unity. Usage: dotnet run -- [seedCount] [anchors: synthetic|realistic]
// or `dotnet run -- econ`: the EconomySimTests 200 seeds, printing the same pacing table.
static class Program
{
    static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "econ") return Econ();
        int count = args.Length > 0 ? int.Parse(args[0]) : 200;
        bool realistic = args.Length > 1 && args[1] == "realistic";
        var input = new MapGenInput
        {
            hubCenter = realistic ? new Vector3(-12.3f, 0, -11.8f) : new Vector3(-12, 0, -12),
            anchors = realistic
                ? new List<MapAnchor> { A(-5f, 9.37f, Vector3.forward), A(6.21f, -12f, Vector3.right), A(-30.7f, -30f, Vector3.left) }
                : new List<MapAnchor> { A(-5f, 10f, Vector3.forward), A(6f, -12f, Vector3.right), A(-30f, -30f, Vector3.left) }
        };
        var seeds = new List<int> { 0, -1, int.MaxValue, int.MinValue };
        for (int s = 1; s <= count; s++) seeds.Add(s);
        var reasons = new Dictionary<string, int>();
        int failed = 0, retried = 0;
        long tiles = 0, gaps = 0, souls = 0;
        var watch = Stopwatch.StartNew();
        foreach (int seed in seeds)
        {
            input.seed = seed;
            var layout = MapGen.Generate(input);
            tiles += layout.tiles.Count;
            gaps += layout.gaps.Count;
            foreach (var spawn in layout.soulSpawns) souls += spawn.baseCount;
            if (layout.effectiveSeed != seed) retried++;
            var problems = MapValidator.Validate(layout);
            if (problems.Count == 0) continue;
            failed++;
            foreach (var p in problems)
            {
                string key = System.Text.RegularExpressions.Regex.Replace(p, @"\d+", "#");
                reasons[key] = reasons.TryGetValue(key, out int n) ? n + 1 : 1;
            }
            if (failed <= 3) Console.WriteLine($"seed {seed} (effective {layout.effectiveSeed}): {string.Join("; ", problems)}");
        }
        Console.WriteLine($"{seeds.Count} seeds, {failed} invalid, {retried} needed retries, {watch.ElapsedMilliseconds} ms");
        Console.WriteLine($"per map: {tiles / seeds.Count} tiles, {gaps / (float)seeds.Count:0.0} dash gaps, {souls / seeds.Count} spawn souls");
        foreach (var kv in reasons) Console.WriteLine($"  {kv.Value}x {kv.Key}");
        return failed == 0 ? 0 : 1;
    }

    static int Econ()
    {
        var results = new List<EconomySimResult>();
        for (int i = 0; i < 200; i++) results.Add(EconomySim.Run(i * 131 + 3)); // EconomySimTests' seeds
        int blocked = results.FindAll(r => r.blocked).Count;
        Console.WriteLine($"blocked seeds: {blocked}");
        foreach (var r in results) if (r.blocked) Console.WriteLine($"  seed {r.seed} after {r.expeditions} expeditions");
        Console.WriteLine($"{"milestone",-16}{"min",8}{"median",8}{"p90",8}{"max",8}{"never",8}");
        Row("first purchase", results.ConvertAll(r => r.minutesFirstPurchase));
        Row("Raiz I", results.ConvertAll(r => r.minutesRaiz1));
        Row("Raiz II", results.ConvertAll(r => r.minutesRaiz2));
        Row("Raiz III", results.ConvertAll(r => r.minutesRaiz3));
        Row("Despertar", results.ConvertAll(r => r.minutesDespertar));
        Row("Nucleo broken", results.ConvertAll(r => r.minutesNucleoBroken));
        var expeditions = results.ConvertAll(r => (float)r.expeditions);
        expeditions.Sort();
        Console.WriteLine($"expeditions: min={expeditions[0]} median={expeditions[100]} p90={expeditions[179]} max={expeditions[199]}");
        return blocked == 0 ? 0 : 1;
    }

    static void Row(string label, List<float> values)
    {
        var reached = values.FindAll(v => v >= 0f);
        reached.Sort();
        if (reached.Count == 0) { Console.WriteLine($"{label,-16}  (never reached)"); return; }
        float P(float p) => reached[(int)(p * (reached.Count - 1) + .5f)];
        Console.WriteLine($"{label,-16}{reached[0],8:0.00}{P(.5f),8:0.00}{P(.9f),8:0.00}{reached[reached.Count - 1],8:0.00}{values.Count - reached.Count,8}");
    }

    static MapAnchor A(float x, float z, Vector3 dir) => new MapAnchor { position = new Vector3(x, 0, z), direction = dir };
}
