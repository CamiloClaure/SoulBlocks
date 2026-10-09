using System.Collections.Generic;
using UnityEngine;

namespace SoulBlocks
{
    // Pure checks on a finished MapLayout. MapGen.Generate() retries seed+1 while this returns
    // any failure, up to Balance.MaxGenerationAttempts times.
    public static class MapValidator
    {
        public static List<string> Validate(MapLayout layout)
        {
            var failures = new List<string>();
            CheckNoDuplicateCells(layout, failures);
            CheckIdsUnique(layout, failures);
            CheckCrossReferences(layout, failures);
            CheckBranchContinuity(layout, failures);
            CheckReachability(layout, failures);
            CheckTierCoverage(layout, failures);
            CheckGaps(layout, failures);
            CheckShelterSpacing(layout, failures);
            CheckBranchLengths(layout, failures);
            if (layout.branches.Count > 1 && layout.connections.Count == 0)
                failures.Add("no connection links the branches into a circuit");
            return failures;
        }

        // The walk can rarely get stuck (occupancy/hub-distance both blocked) and stop short;
        // that is exactly the case Generate() should retry on, so it must show up as a failure.
        static void CheckBranchLengths(MapLayout layout, List<string> failures)
        {
            foreach (var branch in layout.branches)
            {
                float min, max;
                switch (branch.specialty)
                {
                    case BranchSpecialty.Souls: min = Balance.SoulsBranchMin; max = Balance.SoulsBranchMax; break;
                    case BranchSpecialty.Prisons: min = Balance.PrisonsBranchMin; max = Balance.PrisonsBranchMax; break;
                    default: min = Balance.ExplorationBranchMin; max = Balance.ExplorationBranchMax; break;
                }
                if (branch.length < min - Balance.TileSize - .01f || branch.length > max + .01f)
                    failures.Add("branch " + branch.id + " length " + branch.length + " is outside its specialty's table");
            }
        }

        static void CheckNoDuplicateCells(MapLayout layout, List<string> failures)
        {
            var seen = new HashSet<(int, int)>();
            foreach (var tile in layout.tiles)
            {
                var cell = (Mathf.RoundToInt(tile.position.x * 100f), Mathf.RoundToInt(tile.position.z * 100f));
                if (!seen.Add(cell)) failures.Add("duplicate tile cell at index " + tile.index);
            }
        }

        static void CheckIdsUnique(MapLayout layout, List<string> failures)
        {
            CheckUnique(Ids(layout.branches, b => b.id), "branches", failures);
            CheckUnique(Ids(layout.tiles, t => t.index), "tiles", failures);
            CheckUnique(Ids(layout.prisons, p => p.id), "prisons", failures);
            CheckUnique(Ids(layout.soulSpawns, s => s.id), "soulSpawns", failures);
            CheckUnique(Ids(layout.shelters, s => s.id), "shelters", failures);
            CheckUnique(Ids(layout.gaps, g => g.id), "gaps", failures);
            CheckUnique(Ids(layout.connections, c => c.id), "connections", failures);
            CheckUnique(Ids(layout.altars, a => a.id), "altars", failures);
            CheckUnique(Ids(layout.portals, p => p.id), "portals", failures);
        }

        static IEnumerable<int> Ids<T>(List<T> list, System.Func<T, int> get)
        {
            foreach (var item in list) yield return get(item);
        }

        static void CheckUnique(IEnumerable<int> ids, string what, List<string> failures)
        {
            var seen = new HashSet<int>();
            foreach (var id in ids)
                if (!seen.Add(id)) failures.Add("duplicate id in " + what + ": " + id);
        }

        static void CheckCrossReferences(MapLayout layout, List<string> failures)
        {
            int branchCount = layout.branches.Count;
            bool ValidBranch(int id) => id >= 0 && id < branchCount;

            foreach (var branch in layout.branches)
            {
                bool found = false;
                foreach (var prison in layout.prisons)
                    if (prison.id == branch.endPrisonId && prison.branchEnd) found = true;
                if (branch.endPrisonId >= 0 && !found) failures.Add("branch " + branch.id + " endPrisonId does not resolve to a branchEnd prison");
            }
            foreach (var prison in layout.prisons)
                if (!ValidBranch(prison.branchId)) failures.Add("prison " + prison.id + " has invalid branchId");
            foreach (var soul in layout.soulSpawns)
                if (!ValidBranch(soul.branchId)) failures.Add("soul spawn " + soul.id + " has invalid branchId");
            foreach (var shelter in layout.shelters)
                if (!ValidBranch(shelter.branchId)) failures.Add("shelter " + shelter.id + " has invalid branchId");
            foreach (var altar in layout.altars)
                if (!ValidBranch(altar.branchId)) failures.Add("altar " + altar.id + " has invalid branchId");
            foreach (var portal in layout.portals)
                if (!ValidBranch(portal.branchId)) failures.Add("portal " + portal.id + " has invalid branchId");
            foreach (var gap in layout.gaps)
                if (!ValidBranch(gap.branchId)) failures.Add("gap " + gap.id + " has invalid branchId");
            foreach (var connection in layout.connections)
            {
                if (!ValidBranch(connection.fromBranch) || !ValidBranch(connection.toBranch))
                    failures.Add("connection " + connection.id + " has invalid branch reference");
                foreach (var tileIndex in connection.tiles)
                {
                    bool found = false;
                    foreach (var tile in layout.tiles)
                        if (tile.index == tileIndex) { found = true; break; }
                    if (!found) failures.Add("connection " + connection.id + " references missing tile " + tileIndex);
                }
            }
        }

        static void CheckBranchContinuity(MapLayout layout, List<string> failures)
        {
            foreach (var branch in layout.branches)
            {
                var distances = new List<float>();
                foreach (var tile in layout.tiles)
                    if (tile.branchId == branch.id && !distances.Contains(tile.pathDistance)) distances.Add(tile.pathDistance);
                distances.Sort();
                if (distances.Count == 0) { failures.Add("branch " + branch.id + " has no tiles"); continue; }
                if (Mathf.Abs(distances[0]) > .01f)
                    failures.Add("branch " + branch.id + " required path does not start at distance 0");
                for (int i = 1; i < distances.Count; i++)
                    if (Mathf.Abs(distances[i] - distances[i - 1] - Balance.TileSize) > .01f)
                        failures.Add("branch " + branch.id + " required path breaks continuity at distance " + distances[i - 1]);
            }
        }

        static void CheckReachability(MapLayout layout, List<string> failures)
        {
            float radius = Balance.EnergyMax * Balance.MoveSpeed / 2f;
            float requiredSouls = 1.5f * Balance.ValidatorFirstUpgradeCost;

            int reachableSouls = 0;
            foreach (var soul in layout.soulSpawns)
                if (MapGen.PathDistanceAt(layout, soul.branchId, soul.position) <= radius) reachableSouls += soul.baseCount;
            if (reachableSouls < requiredSouls)
                failures.Add("souls reachable within the base round trip (" + reachableSouls + ") fall short of " + requiredSouls);

            bool anyT1 = false;
            foreach (var prison in layout.prisons)
                if (prison.tier == PrisonTier.T1 && MapGen.PathDistanceAt(layout, prison.branchId, prison.position) <= radius) anyT1 = true;
            if (!anyT1) failures.Add("no T1 prison reachable within the base round trip");
        }

        static void CheckTierCoverage(MapLayout layout, List<string> failures)
        {
            bool t1 = false, t2 = false, t3 = false, t4 = false;
            int nucleoCount = 0;
            foreach (var prison in layout.prisons)
            {
                bool deep = prison.requiresUnlock == UnlockIds.Raiz3 || prison.requiresUnlock == UnlockIds.Despertar;
                if (prison.tier == PrisonTier.T1 && !deep) t1 = true;
                if (prison.tier == PrisonTier.T2 && !deep) t2 = true;
                if (prison.tier == PrisonTier.T3 && !deep) t3 = true;
                if (prison.tier == PrisonTier.T4 && deep) t4 = true;
                if (prison.tier == PrisonTier.Nucleo) nucleoCount++;
            }
            if (!t1) failures.Add("no T1 prison in the non-deep map");
            if (!t2) failures.Add("no T2 prison in the non-deep map");
            if (!t3) failures.Add("no T3 prison in the non-deep map");
            if (!t4) failures.Add("no T4 prison in the deep segments");
            if (nucleoCount != 1) failures.Add("expected exactly one Nucleo prison, found " + nucleoCount);
        }

        static void CheckGaps(MapLayout layout, List<string> failures)
        {
            foreach (var gap in layout.gaps)
            {
                float dx = gap.from.x - gap.to.x, dz = gap.from.z - gap.to.z;
                float dist = Mathf.Sqrt(dx * dx + dz * dz);
                if (dist > Balance.GapMaxLength + .01f) failures.Add("gap " + gap.id + " exceeds the max dash-shortcut length");

                bool landsOnPlatform = false;
                foreach (var tile in layout.tiles)
                {
                    if (!tile.shortcut) continue;
                    float px = tile.position.x - gap.to.x, pz = tile.position.z - gap.to.z;
                    if (Mathf.Sqrt(px * px + pz * pz) <= Balance.PlatformSize * .5f + .01f) { landsOnPlatform = true; break; }
                }
                if (!landsOnPlatform) failures.Add("gap " + gap.id + " does not land on a shortcut platform");
            }

            // The required path is every tile not flagged shortcut=true; platforms only ever add
            // a tile beside the path, so the required path never depends on a gap.
        }

        static void CheckShelterSpacing(MapLayout layout, List<string> failures)
        {
            int explorationId = -1;
            foreach (var branch in layout.branches)
                if (branch.specialty == BranchSpecialty.Exploration) explorationId = branch.id;
            if (explorationId < 0) return;

            var distances = new List<float>();
            foreach (var shelter in layout.shelters)
                if (shelter.branchId == explorationId) distances.Add(MapGen.PathDistanceAt(layout, shelter.branchId, shelter.position));
            distances.Sort();
            for (int i = 1; i < distances.Count; i++)
                if (distances[i] - distances[i - 1] > Balance.ShelterSpacing * 1.5f)
                    failures.Add("shelter spacing on the exploration branch exceeds the bound");
        }
    }
}
