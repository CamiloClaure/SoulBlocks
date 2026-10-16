using System.Collections.Generic;
using UnityEngine;

namespace SoulBlocks
{
    // Pure planning for the next expedition's 3 objectives and anomaly (section 1.8 of the
    // plan). Everything is deterministic from (Run.seed, expeditionIndex):
    // same inputs draw the same SeededRng sequence in the same order, so the same plan comes out
    // every time. Runtime/ExpeditionPlanning/ExpeditionPlanningSystem wires this into
    // GameEvents, the static hooks and the HUD.
    public static class ExpeditionPlanner
    {
        public static ExpeditionPlan Plan(MapLayout layout, RunState run, StatSheet stats, int expeditionIndex)
        {
            var plan = new ExpeditionPlan { chosenObjective = -1 };
            if (layout == null || layout.branches.Count == 0) return plan; // ponytail: no map yet

            int seed = run.seed;
            var rng = SeededRng.Stream(seed, "azar.expedicion." + expeditionIndex);
            int activeBranch = ActiveBranch(layout, seed, expeditionIndex);
            plan.hintBranch = activeBranch;

            var objectives = new List<ObjectiveRecord> { RollRecolectar(stats, activeBranch) };

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
                objectives.Add(RollRecolectar(stats, branch));
            }

            plan.offered.AddRange(objectives);
            plan.anomaly = RollAnomaly(layout, run, activeBranch, rng);
            return plan;
        }

        // The branch the anomaly and the HUD hint point at this expedition.
        public static int ActiveBranch(MapLayout layout, int seed, int expeditionIndex)
        {
            if (layout == null || layout.branches.Count == 0) return -1;
            return SeededRng.Stream(seed, "azar.density.branch." + expeditionIndex).Next(0, layout.branches.Count);
        }

        static float ReachBudget(StatSheet stats) => stats.Get(StatId.EnergyMax) * stats.Get(StatId.MoveSpeed) * .5f;

        // Deliver souls: a share of what the torch can lead at once, so one good trip does it.
        static ObjectiveRecord RollRecolectar(StatSheet stats, int branchId)
        {
            int capacity = Mathf.Max(1, stats.GetInt(StatId.SpiritCapacity));
            int goal = Mathf.Clamp(Mathf.RoundToInt(capacity * Balance.ObjectiveRecolectarFraction), (int)Balance.ObjectiveRecolectarMin, capacity);
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
            int pickLevel = PrisonRules.PickaxeTier(run.pickaxeLevel);
            float budget = ReachBudget(stats);
            var candidates = new List<PrisonLayout>();
            foreach (var prison in layout.prisons)
            {
                if (run.Prison(prison.id).broken) continue;
                if (!string.IsNullOrEmpty(prison.requiresUnlock) && !run.HasUnlock(prison.requiresUnlock)) continue;
                if (PrisonRules.RequiredLevel(prison.tier) > pickLevel) continue;
                if (layout.PathDistanceAt(prison.branchId, prison.position) > budget) continue;
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
                if (layout.PathDistanceAt(shelter.branchId, shelter.position) > budget) continue;
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
            return null; // ponytail: only reachable if the layout has no prisons or tiles at all
        }

        static AnomalyRecord TryBuildAnomaly(string kind, MapLayout layout, RunState run, int activeBranch, SeededRng rng)
        {
            switch (kind)
            {
                case "almas":
                case "corriente":
                {
                    // A burst of souls (almas) or a dash current (corriente) on a tile, the
                    // active branch first.
                    var onActive = new List<TileLayout>();
                    var any = new List<TileLayout>();
                    foreach (var tile in layout.tiles)
                    {
                        if (tile.branchId < 0) continue;
                        if (!string.IsNullOrEmpty(tile.requiresUnlock) && !run.HasUnlock(tile.requiresUnlock)) continue;
                        any.Add(tile);
                        if (tile.branchId == activeBranch) onActive.Add(tile);
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
                default:
                    return null;
            }
        }
    }
}
