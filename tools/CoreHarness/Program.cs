using System;
using System.Collections.Generic;
using System.Diagnostics;
using SoulBlocks;
using UnityEngine;

// Runs the seeded map generator + validator outside Unity. Usage: dotnet run -- [seedCount] [anchors: synthetic|realistic]
static class Program
{
    static int Main(string[] args)
    {
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
        var watch = Stopwatch.StartNew();
        foreach (int seed in seeds)
        {
            input.seed = seed;
            var layout = MapGen.Generate(input);
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
        foreach (var kv in reasons) Console.WriteLine($"  {kv.Value}x {kv.Key}");
        return failed == 0 ? 0 : 1;
    }

    static MapAnchor A(float x, float z, Vector3 dir) => new MapAnchor { position = new Vector3(x, 0, z), direction = dir };
}
