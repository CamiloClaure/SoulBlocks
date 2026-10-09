using System.Collections.Generic;
using UnityEngine;

namespace SoulBlocks
{
    // Pure planning for the next expedition's 3 objectives, anomaly and soul density (section
    // 1.8 of the plan). Everything is deterministic from (Run.effectiveSeed, expeditionIndex):
    // same inputs draw the same SeededRng sequence in the same order, so the same plan comes out
    // every time. Runtime/ExpeditionPlanning/ExpeditionPlanningSystem wires this into
    // GameEvents, the static hooks and the HUD.
    public static class ExpeditionPlanner
    {
        public static ExpeditionPlan Plan(MapLayout layout, RunState run, StatSheet stats, int expeditionIndex)
        {
            var plan = new ExpeditionPlan { chosenObjective = -1 };
            if (layout == null || layout.branches.Count == 0) return plan; // ponytail: no map yet

            int seed = run.effectiveSeed;
            var rng = SeededRng.Stream(seed, "azar.expedicion." + expeditionIndex);
            int activeBranch = ActiveBranch(layout, seed, expeditionIndex);
            plan.hintBranch = activeBranch;

            var objectives = new List<ObjectiveRecord> { RollRecolectar(layout, run, seed, expeditionIndex, stats, activeBranch) };

            var fracturar = RollFracturar(layout, run, stats, rng);
            if (fracturar != null) objectives.Add(fracturar);

            var descubrir = RollDescubrir(layout, run, stats, rng);
            if (descubrir != null) objectives.Add(descubrir);

            // Always 3 offered: if a kind had no valid target this time (section 1.8, "else
            // another kind"), top up with recolectar on branches not already offered.
            var usedBranches = new HashSet<int> { activeBranch };
            while (objectives.Count < 3)
            {
                int branch = PickUnusedBranch(layout, usedBranches, rng);
                usedBranches.Add(branch);
                objectives.Add(RollRecolectar(layout, run, seed, expeditionIndex, stats, branch));
            }

            plan.offered.AddRange(objectives);
            plan.anomaly = RollAnomaly(layout, run, activeBranch, rng);
            return plan;
        }

        // Souls spawned for this SoulSpawnLayout, for the given expedition: ExpeditionController
        // .SpawnCountFor's density hook. Deterministic from (seed, expeditionIndex) alone, not
        // from Run.plan, so it gives the exact same number whether it is asked before or after
        // Plan() above rolls the plan for that same index (no ordering dependency between the
        // two systems that call it: SoulSpawner on MapBuilt/ExpeditionEnded, us on the HUD hint).
        // ponytail: recomputes ActiveBranch/GuaranteeScale (O(spawn count)) on every call instead
        // of caching per expedition; fine at obra-gruesa's dozens-of-spawns scale.
        public static int SpawnCount(MapLayout layout, RunState run, int seed, int expeditionIndex, SoulSpawnLayout spawn)
        {
            int activeBranch = ActiveBranch(layout, seed, expeditionIndex);
            float scale = GuaranteeScale(layout, run, seed, expeditionIndex);
            float multiplier = BranchMultiplier(seed, expeditionIndex, spawn.branchId, activeBranch) * scale;
            return Mathf.Max(0, Mathf.RoundToInt(spawn.baseCount * multiplier));
        }

        public static int ActiveBranch(MapLayout layout, int seed, int expeditionIndex)
        {
            if (layout == null || layout.branches.Count == 0) return -1;
            return SeededRng.Stream(seed, "azar.density.branch." + expeditionIndex).Next(0, layout.branches.Count);
        }

        static float BranchMultiplier(int seed, int expeditionIndex, int branchId, int activeBranch)
        {
            if (branchId == activeBranch) return Balance.DensityActiveMultiplier;
            var rng = SeededRng.Stream(seed, "azar.density.other." + expeditionIndex + "." + branchId);
            return rng.Range(Balance.DensityOtherMin, Balance.DensityOtherMax);
        }

        // Scales every branch's multiplier up (never down) so the expedition's total souls
        // never fall below Balance.DensityMinimumTotal, counting only spawns unlocked right now.
        static float GuaranteeScale(MapLayout layout, RunState run, int seed, int expeditionIndex)
        {
            int activeBranch = ActiveBranch(layout, seed, expeditionIndex);
            float total = 0f;
            foreach (var spawn in layout.soulSpawns)
            {
                if (!string.IsNullOrEmpty(spawn.requiresUnlock) && !run.HasUnlock(spawn.requiresUnlock)) continue;
                total += spawn.baseCount * BranchMultiplier(seed, expeditionIndex, spawn.branchId, activeBranch);
            }
            if (total <= 0f) return 1f;
            return total < Balance.DensityMinimumTotal ? Balance.DensityMinimumTotal / total : 1f;
        }

        static float ReachBudget(StatSheet stats) => stats.Get(StatId.EnergyMax) * stats.Get(StatId.MoveSpeed) * .5f;

        static ObjectiveRecord RollRecolectar(MapLayout layout, RunState run, int seed, int expeditionIndex, StatSheet stats, int branchId)
        {
            int available = 0;
            foreach (var spawn in layout.soulSpawns)
            {
                if (spawn.branchId != branchId) continue;
                if (!string.IsNullOrEmpty(spawn.requiresUnlock) && !run.HasUnlock(spawn.requiresUnlock)) continue;
                available += SpawnCount(layout, run, seed, expeditionIndex, spawn);
            }
            int capacity = Mathf.Max(1, stats.GetInt(StatId.CargoCapacity));
            int goal = available > 0
                ? Mathf.Clamp(Mathf.RoundToInt(available * Balance.ObjectiveRecolectarFraction), (int)Balance.ObjectiveRecolectarMin, capacity)
                : Mathf.Min((int)Balance.ObjectiveRecolectarMin, capacity);

            return new ObjectiveRecord
            {
                kind = "recolectar",
                branchId = branchId,
                targetId = -1,
                goal = Mathf.Max(1, goal),
                rewardWisdom = (int)Balance.ObjectiveRewardWisdom,
                rewardSouls = (int)Balance.ObjectiveRewardSouls
            };
        }

        // Only a prison the player can damage right now (tier gate) and reach within the base
        // round trip qualifies: "always achievable with current power" (section 1.8).
        static ObjectiveRecord RollFracturar(MapLayout layout, RunState run, StatSheet stats, SeededRng rng)
        {
            int rayLevel = stats.GetInt(StatId.RayLevel);
            float budget = ReachBudget(stats);
            var candidates = new List<PrisonLayout>();
            foreach (var prison in layout.prisons)
            {
                if (run.Prison(prison.id).broken) continue;
                if (!string.IsNullOrEmpty(prison.requiresUnlock) && !run.HasUnlock(prison.requiresUnlock)) continue;
                if (PrisonRules.RequiredLevel(prison.tier) > rayLevel) continue;
                if (MapGen.PathDistanceAt(layout, prison.branchId, prison.position) > budget) continue;
                candidates.Add(prison);
            }
            if (candidates.Count == 0) return null;

            var chosen = rng.Pick(candidates);
            return new ObjectiveRecord
            {
                kind = "fracturar",
                branchId = chosen.branchId,
                targetId = chosen.id,
                goal = PrisonRules.TotalHp(chosen.tier),
                progress = run.Prison(chosen.id).damage,
                rewardWisdom = (int)Balance.ObjectiveRewardWisdom,
                rewardSouls = (int)Balance.ObjectiveRewardSouls
            };
        }

        // Undiscovered shelter (targetId = shelter.id, >= 0) or undiscovered, unlocked
        // connection tile (targetId = -(tileIndex + 1), < 0). ponytail: ObjectiveRecord has no
        // sub-kind field to tell the two apart, so the sign of targetId encodes it; contract
        // request: a dedicated sub-kind/targetIsTile field would drop this.
        static ObjectiveRecord RollDescubrir(MapLayout layout, RunState run, StatSheet stats, SeededRng rng)
        {
            float budget = ReachBudget(stats);
            var candidates = new List<ObjectiveRecord>();

            foreach (var shelter in layout.shelters)
            {
                if (run.visitedShelterIds.Contains(shelter.id)) continue;
                if (MapGen.PathDistanceAt(layout, shelter.branchId, shelter.position) > budget) continue;
                candidates.Add(new ObjectiveRecord { kind = "descubrir", branchId = shelter.branchId, targetId = shelter.id, goal = 1 });
            }

            foreach (var connection in layout.connections)
            {
                if (!string.IsNullOrEmpty(connection.requiresUnlock) && !run.HasUnlock(connection.requiresUnlock)) continue;
                foreach (var tileIndex in connection.tiles)
                {
                    if (run.discoveredTiles.Contains(tileIndex)) continue;
                    if (TilePathDistance(layout, tileIndex) > budget) continue;
                    candidates.Add(new ObjectiveRecord { kind = "descubrir", branchId = connection.fromBranch, targetId = -(tileIndex + 1), goal = 1 });
                    break; // one undiscovered tile is enough to represent this connection
                }
            }

            if (candidates.Count == 0) return null;
            var chosen = rng.Pick(candidates);
            chosen.rewardWisdom = (int)Balance.ObjectiveRewardWisdom;
            chosen.rewardSouls = (int)Balance.ObjectiveRewardSouls;
            return chosen;
        }

        static float TilePathDistance(MapLayout layout, int tileIndex)
        {
            foreach (var tile in layout.tiles)
                if (tile.index == tileIndex) return tile.pathDistance;
            return float.MaxValue;
        }

        static int PickUnusedBranch(MapLayout layout, HashSet<int> used, SeededRng rng)
        {
            var options = new List<int>();
            foreach (var branch in layout.branches)
                if (!used.Contains(branch.id)) options.Add(branch.id);
            return options.Count > 0 ? rng.Pick(options) : layout.branches[rng.Next(0, layout.branches.Count)].id;
        }

        // Exactly one anomaly per expedition (section 1.8): try the 3 kinds in a random order,
        // keep the first that has a valid target in this layout/run.
        static AnomalyRecord RollAnomaly(MapLayout layout, RunState run, int activeBranch, SeededRng rng)
        {
            var kinds = new List<string> { "almas", "prision_debilitada", "corriente" };
            rng.Shuffle(kinds);
            foreach (var kind in kinds)
            {
                var anomaly = TryBuildAnomaly(kind, layout, run, activeBranch, rng);
                if (anomaly != null) return anomaly;
            }
            return null; // ponytail: only reachable if the layout has no soul spawns, prisons or tiles at all
        }

        static AnomalyRecord TryBuildAnomaly(string kind, MapLayout layout, RunState run, int activeBranch, SeededRng rng)
        {
            switch (kind)
            {
                case "almas":
                {
                    var onActive = new List<SoulSpawnLayout>();
                    var any = new List<SoulSpawnLayout>();
                    foreach (var spawn in layout.soulSpawns)
                    {
                        if (!string.IsNullOrEmpty(spawn.requiresUnlock) && !run.HasUnlock(spawn.requiresUnlock)) continue;
                        any.Add(spawn);
                        if (spawn.branchId == activeBranch) onActive.Add(spawn);
                    }
                    var pool = onActive.Count > 0 ? onActive : any;
                    if (pool.Count == 0) return null;
                    var pick = rng.Pick(pool);
                    return new AnomalyRecord { kind = kind, branchId = pick.branchId, position = pick.position };
                }
                case "prision_debilitada":
                {
                    var candidates = new List<PrisonLayout>();
                    foreach (var prison in layout.prisons)
                        if (!run.Prison(prison.id).broken && (string.IsNullOrEmpty(prison.requiresUnlock) || run.HasUnlock(prison.requiresUnlock)))
                            candidates.Add(prison);
                    if (candidates.Count == 0) return null;
                    var pick = rng.Pick(candidates);
                    return new AnomalyRecord { kind = kind, branchId = pick.branchId, position = pick.position };
                }
                case "corriente":
                {
                    var onActive = new List<TileLayout>();
                    var any = new List<TileLayout>();
                    foreach (var tile in layout.tiles)
                    {
                        if (!string.IsNullOrEmpty(tile.requiresUnlock) && !run.HasUnlock(tile.requiresUnlock)) continue;
                        any.Add(tile);
                        if (tile.branchId == activeBranch) onActive.Add(tile);
                    }
                    var pool = onActive.Count > 0 ? onActive : any;
                    if (pool.Count == 0) return null;
                    var pick = rng.Pick(pool);
                    return new AnomalyRecord { kind = kind, branchId = pick.branchId, position = pick.position };
                }
                default:
                    return null;
            }
        }
    }
}
