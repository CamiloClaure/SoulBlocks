using System.Collections.Generic;
using UnityEngine;

namespace SoulBlocks
{
    public static partial class MapGen
    {
        // One step of a branch's polyline walk, kept only while generating this branch (not
        // part of the serialized MapLayout).
        struct Step
        {
            public Vector3 pos;
            public Vector3 heading;
            public bool deep;
        }

        public static MapLayout Generate(MapGenInput input)
        {
            if (input.anchors == null || input.anchors.Count == 0)
                return new MapLayout { seed = input.seed, effectiveSeed = input.seed, hubCenter = input.hubCenter };

            MapLayout lastAttempt = null;
            int lastSeed = input.seed;
            for (int attempt = 0; attempt < Balance.MaxGenerationAttempts; attempt++)
            {
                int trySeed = input.seed + attempt;
                var layout = TryGenerate(input, trySeed);
                lastAttempt = layout;
                lastSeed = trySeed;
                if (MapValidator.Validate(layout).Count == 0) return layout;
            }
            // ponytail: 50 misses in a row should not happen with open anchors; ship the last
            // attempt rather than a null layout, the orchestrator's balance pass can widen the
            // table if some real seed keeps landing here.
            lastAttempt.effectiveSeed = lastSeed;
            return lastAttempt;
        }

        static MapLayout TryGenerate(MapGenInput input, int seed)
        {
            var layout = new MapLayout { seed = input.seed, effectiveSeed = seed, hubCenter = input.hubCenter };
            var occupied = new HashSet<(int, int)>();
            int tileIndex = 0, prisonId = 0, soulId = 0, shelterId = 0, gapId = 0, groupId = 0,
                connectionId = 0, altarId = 0, portalId = 0;

            var specialties = new List<BranchSpecialty> { BranchSpecialty.Souls, BranchSpecialty.Prisons, BranchSpecialty.Exploration };
            SeededRng.Stream(seed, "map.specialty").Shuffle(specialties);
            int nucleoBranch = SeededRng.Stream(seed, "map.nucleo").Next(0, input.anchors.Count);

            for (int b = 0; b < input.anchors.Count; b++)
            {
                var specialty = specialties[b % specialties.Count];
                BuildBranch(layout, b, specialty, input.anchors[b], input.hubCenter, seed, occupied,
                    b == nucleoBranch, ref tileIndex, ref prisonId, ref soulId, ref shelterId,
                    ref gapId, ref groupId, ref altarId, ref portalId);
            }

            for (int a = 0; a < layout.branches.Count; a++)
                for (int c = a + 1; c < layout.branches.Count; c++)
                    TryConnect(layout, occupied, input.hubCenter, a, c, ref tileIndex, ref connectionId);

            return layout;
        }

        static void BuildBranch(MapLayout layout, int branchId, BranchSpecialty specialty, MapAnchor anchor,
            Vector3 hubCenter, int seed, HashSet<(int, int)> occupied, bool isNucleoBranch,
            ref int tileIndex, ref int prisonId, ref int soulId, ref int shelterId, ref int gapId,
            ref int groupId, ref int altarId, ref int portalId)
        {
            string prefix = "map.branch." + branchId;
            float minLen, maxLen;
            switch (specialty)
            {
                case BranchSpecialty.Souls: minLen = Balance.SoulsBranchMin; maxLen = Balance.SoulsBranchMax; break;
                case BranchSpecialty.Prisons: minLen = Balance.PrisonsBranchMin; maxLen = Balance.PrisonsBranchMax; break;
                default: minLen = Balance.ExplorationBranchMin; maxLen = Balance.ExplorationBranchMax; break;
            }
            float lengthMeters = SeededRng.Stream(seed, prefix + ".length").Range(minLen, maxLen);
            int baseStepCount = Mathf.Max(3, Mathf.RoundToInt(lengthMeters / Balance.TileSize));
            int deepStepCount = Mathf.Max(1, Mathf.RoundToInt(Balance.DeepExtensionLength / Balance.TileSize));
            int totalSteps = baseStepCount + deepStepCount;

            var walkRng = SeededRng.Stream(seed, prefix + ".walk");
            var steps = WalkBranch(anchor, totalSteps, hubCenter, occupied, walkRng);
            int actualBase = Mathf.Min(baseStepCount, steps.Count);
            for (int i = actualBase; i < steps.Count; i++)
            {
                var s = steps[i];
                s.deep = true;
                steps[i] = s;
            }

            var branch = new BranchLayout
            {
                id = branchId,
                specialty = specialty,
                anchor = anchor.position,
                direction = anchor.direction,
                length = actualBase * Balance.TileSize
            };
            layout.branches.Add(branch);

            var mainTiles = new List<TileLayout>();
            for (int k = 0; k < steps.Count; k++)
            {
                var tile = new TileLayout
                {
                    index = tileIndex++,
                    position = steps[k].pos,
                    branchId = branchId,
                    pathDistance = (k + .5f) * Balance.TileSize,
                    requiresUnlock = steps[k].deep ? UnlockIds.Raiz3 : ""
                };
                layout.tiles.Add(tile);
                mainTiles.Add(tile);
            }

            // One props stream per branch, threaded through every prison placed on it: each
            // Chance() call must consume the next draw, not a fresh one, or separate prisons end
            // up re-rolling the same first value instead of independent ones.
            var propsRng = SeededRng.Stream(seed, prefix + ".props");

            PrisonTier endTier;
            switch (specialty)
            {
                case BranchSpecialty.Souls: endTier = PrisonTier.T1; AddSoulsInteriorPrisons(layout, branch, steps, ref prisonId, propsRng); break;
                case BranchSpecialty.Prisons: endTier = PrisonTier.T2; AddPrisonGroups(layout, branch, steps, hubCenter, occupied, seed, propsRng, ref tileIndex, ref prisonId, ref groupId); break;
                default: endTier = PrisonTier.T3; break;
            }

            if (actualBase > 0)
            {
                var endStep = steps[actualBase - 1];
                var endPrison = new PrisonLayout
                {
                    id = prisonId++,
                    position = endStep.pos,
                    tier = endTier,
                    pattern = PrisonPattern.Single,
                    groupId = -1,
                    branchId = branchId,
                    branchEnd = true
                };
                ApplyProps(endPrison, PrisonPattern.Single, propsRng);
                layout.prisons.Add(endPrison);
                branch.endPrisonId = endPrison.id;
                layout.portals.Add(new PortalLayout { id = portalId++, position = endStep.pos, branchId = branchId });
            }

            if (steps.Count > actualBase)
                AddDeepPrisons(layout, branch, steps, actualBase, isNucleoBranch, propsRng, ref prisonId);

            AddSoulSpawns(layout, branch, steps, specialty, ref soulId);

            if (specialty == BranchSpecialty.Exploration)
            {
                AddShelters(layout, branch, steps, actualBase, ref shelterId);
                AddAltars(layout, branch, steps, actualBase, seed, ref altarId);
            }
            else if (specialty == BranchSpecialty.Prisons && actualBase > 2)
            {
                var nearEnd = steps[actualBase - 2];
                layout.shelters.Add(new ShelterLayout { id = shelterId++, position = nearEnd.pos, radius = Balance.ShelterRadius, branchId = branch.id });
            }

            AddShortcuts(mainTiles, branchId, SeededRng.Stream(seed, prefix + ".shortcuts"), layout.gaps, ref gapId);
        }

        static List<Step> WalkBranch(MapAnchor anchor, int totalSteps, Vector3 hubCenter, HashSet<(int, int)> occupied, SeededRng rng)
        {
            var steps = new List<Step>();
            Vector3 heading = Cardinal(anchor.direction);
            Vector3 pos = anchor.position + heading * (Balance.TileSize * .5f); // flush with the island edge
            occupied.Add(CellOf(pos, hubCenter));
            steps.Add(new Step { pos = pos, heading = heading, deep = false });

            for (int t = 1; t < totalSteps; t++)
            {
                bool forceStraight = t < 3;
                Vector3 nextHeading;
                Vector3 next;
                if (forceStraight)
                {
                    next = pos + heading * Balance.TileSize;
                    nextHeading = heading;
                    if (!IsValidStep(pos, next, hubCenter, occupied)) break;
                }
                else
                {
                    next = StepCandidate(pos, heading, rng, occupied, hubCenter, out nextHeading);
                    if ((next - pos).sqrMagnitude < .0001f) break; // stuck: end the branch here
                }
                pos = next;
                heading = nextHeading;
                occupied.Add(CellOf(pos, hubCenter));
                steps.Add(new Step { pos = pos, heading = heading, deep = false });
            }
            return steps;
        }

        static Vector3 StepCandidate(Vector3 pos, Vector3 heading, SeededRng rng, HashSet<(int, int)> occupied, Vector3 hubCenter, out Vector3 newHeading)
        {
            Vector3 left = TurnLeft(heading), right = TurnRight(heading);
            Vector3[] order;
            if (rng.Chance(Balance.TurnChance))
                order = rng.Chance(.5f) ? new[] { left, heading, right } : new[] { right, heading, left };
            else
                order = new[] { heading, left, right };

            foreach (var candidate in order)
            {
                Vector3 next = pos + candidate * Balance.TileSize;
                if (IsValidStep(pos, next, hubCenter, occupied)) { newHeading = candidate; return next; }
            }
            newHeading = heading;
            return pos;
        }

        static bool IsValidStep(Vector3 pos, Vector3 next, Vector3 hubCenter, HashSet<(int, int)> occupied)
        {
            if (occupied.Contains(CellOf(next, hubCenter))) return false;
            return XZDist(next, hubCenter) >= XZDist(pos, hubCenter) - .001f;
        }

        static Vector3 Cardinal(Vector3 dir)
        {
            if (dir.sqrMagnitude < .0001f) return Vector3.forward;
            return Mathf.Abs(dir.x) >= Mathf.Abs(dir.z) ? new Vector3(Mathf.Sign(dir.x), 0, 0) : new Vector3(0, 0, Mathf.Sign(dir.z));
        }

        static Vector3 TurnLeft(Vector3 heading) => new Vector3(-heading.z, 0, heading.x);
        static Vector3 TurnRight(Vector3 heading) => new Vector3(heading.z, 0, -heading.x);

        static (int, int) CellOf(Vector3 pos, Vector3 origin) =>
            (Mathf.RoundToInt((pos.x - origin.x) / Balance.TileSize), Mathf.RoundToInt((pos.z - origin.z) / Balance.TileSize));

        static float XZDist(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x, dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        internal static float PathDistanceAt(MapLayout layout, int branchId, Vector3 position)
        {
            foreach (var tile in layout.tiles)
                if (tile.branchId == branchId && (tile.position - position).sqrMagnitude < .01f) return tile.pathDistance;
            return 0f;
        }

        static void AddSoulsInteriorPrisons(MapLayout layout, BranchLayout branch, List<Step> steps, ref int prisonId, SeededRng propsRng)
        {
            for (int k = 3; k < steps.Count - 1 && k < branch.length / Balance.TileSize - 1; k += Balance.SoulsBranchPrisonInterval)
            {
                var prison = new PrisonLayout
                {
                    id = prisonId++,
                    position = steps[k].pos,
                    tier = PrisonTier.T1,
                    pattern = PrisonPattern.Single,
                    groupId = -1,
                    branchId = branch.id,
                    requiresUnlock = steps[k].deep ? UnlockIds.Raiz3 : ""
                };
                ApplyProps(prison, PrisonPattern.Single, propsRng);
                layout.prisons.Add(prison);
            }
        }

        static readonly PrisonTier[] GroupTiers = { PrisonTier.T1, PrisonTier.T2, PrisonTier.T3 };

        static void AddPrisonGroups(MapLayout layout, BranchLayout branch, List<Step> steps, Vector3 hubCenter,
            HashSet<(int, int)> occupied, int seed, SeededRng propsRng, ref int tileIndex, ref int prisonId, ref int groupId)
        {
            int baseSteps = Mathf.RoundToInt(branch.length / Balance.TileSize);
            var patterns = new List<PrisonPattern> { PrisonPattern.Line, PrisonPattern.Arc, PrisonPattern.Compact, PrisonPattern.Connected };
            SeededRng.Stream(seed, "map.branch." + branch.id + ".patterns").Shuffle(patterns);

            for (int g = 0; g < GroupTiers.Length; g++)
            {
                float frac = (g + 1) / (GroupTiers.Length + 1.5f);
                int idx = Mathf.Clamp(Mathf.RoundToInt(frac * (baseSteps - 1)), 3, Mathf.Max(3, baseSteps - 2));
                if (idx >= steps.Count) continue;
                var center = steps[idx];
                var pattern = patterns[g % patterns.Count];
                string unlock = center.deep ? UnlockIds.Raiz3 : "";
                var positions = PlacePlazaAndGetPrisonPositions(center, pattern, hubCenter, occupied, layout, branch.id, ref tileIndex, (idx + .5f) * Balance.TileSize, unlock);
                int thisGroup = groupId++;
                foreach (var pos in positions)
                {
                    var prison = new PrisonLayout
                    {
                        id = prisonId++,
                        position = pos,
                        tier = GroupTiers[g],
                        pattern = pattern,
                        groupId = thisGroup,
                        branchId = branch.id,
                        requiresUnlock = unlock
                    };
                    ApplyProps(prison, pattern, propsRng);
                    layout.prisons.Add(prison);
                }
            }
        }

        static List<Vector3> PlacePlazaAndGetPrisonPositions(Step center, PrisonPattern pattern, Vector3 hubCenter,
            HashSet<(int, int)> occupied, MapLayout layout, int branchId, ref int tileIndex, float pathDistance, string requiresUnlock)
        {
            Vector3 forward = center.heading, right = TurnRight(forward);
            for (int r = -1; r <= 1; r++)
                for (int f = -1; f <= 1; f++)
                {
                    if (r == 0 && f == 0) continue;
                    Vector3 cellPos = center.pos + right * (r * Balance.TileSize) + forward * (f * Balance.TileSize);
                    var cell = CellOf(cellPos, hubCenter);
                    if (occupied.Contains(cell)) continue;
                    occupied.Add(cell);
                    layout.tiles.Add(new TileLayout { index = tileIndex++, position = cellPos, branchId = branchId, pathDistance = pathDistance, requiresUnlock = requiresUnlock });
                }

            var positions = new List<Vector3>();
            foreach (var offset in PatternOffsets(pattern))
                positions.Add(offset.x == 0 && offset.y == 0 ? center.pos : center.pos + right * (offset.x * Balance.TileSize) + forward * (offset.y * Balance.TileSize));
            return positions;
        }

        static List<Vector2Int> PatternOffsets(PrisonPattern pattern)
        {
            switch (pattern)
            {
                case PrisonPattern.Line: return new List<Vector2Int> { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0) };
                case PrisonPattern.Arc: return new List<Vector2Int> { new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(1, 0) };
                case PrisonPattern.Compact: return new List<Vector2Int> { new Vector2Int(-1, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };
                case PrisonPattern.Connected: return new List<Vector2Int> { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(0, 1) };
                default: return new List<Vector2Int> { new Vector2Int(0, 0) };
            }
        }

        static void ApplyProps(PrisonLayout prison, PrisonPattern pattern, SeededRng rng)
        {
            if (pattern == PrisonPattern.Connected) prison.props |= PrisonProp.Resonant;
            if (prison.tier != PrisonTier.T1 && rng.Chance(Balance.ExposedCoreChance)) prison.props |= PrisonProp.ExposedCore;
        }

        static void AddDeepPrisons(MapLayout layout, BranchLayout branch, List<Step> steps, int actualBase, bool isNucleoBranch, SeededRng propsRng, ref int prisonId)
        {
            int deepCount = steps.Count - actualBase;
            int midIndex = actualBase + Mathf.Clamp(deepCount / 2, 0, deepCount - 1);
            var t4 = new PrisonLayout
            {
                id = prisonId++,
                position = steps[midIndex].pos,
                tier = PrisonTier.T4,
                pattern = PrisonPattern.Single,
                groupId = -1,
                branchId = branch.id,
                requiresUnlock = UnlockIds.Raiz3
            };
            ApplyProps(t4, PrisonPattern.Single, propsRng);
            layout.prisons.Add(t4);

            if (!isNucleoBranch) return;
            var lastStep = steps[steps.Count - 1];
            layout.prisons.Add(new PrisonLayout
            {
                id = prisonId++,
                position = lastStep.pos,
                tier = PrisonTier.Nucleo,
                pattern = PrisonPattern.Single,
                groupId = -1,
                branchId = branch.id,
                requiresUnlock = UnlockIds.Despertar
            });
        }

        static void AddSoulSpawns(MapLayout layout, BranchLayout branch, List<Step> steps, BranchSpecialty specialty, ref int soulId)
        {
            int spacing;
            switch (specialty)
            {
                case BranchSpecialty.Souls: spacing = Balance.SoulSpacingSouls; break;
                case BranchSpecialty.Prisons: spacing = Balance.SoulSpacingPrisons; break;
                default: spacing = Balance.SoulSpacingExploration; break;
            }
            for (int k = 1; k < steps.Count; k += spacing)
            {
                layout.soulSpawns.Add(new SoulSpawnLayout
                {
                    id = soulId++,
                    position = steps[k].pos,
                    branchId = branch.id,
                    baseCount = steps[k].deep ? Balance.SoulBaseCountDeep : Balance.SoulBaseCountNormal,
                    requiresUnlock = steps[k].deep ? UnlockIds.Raiz3 : ""
                });
            }
        }

        static void AddShelters(MapLayout layout, BranchLayout branch, List<Step> steps, int actualBase, ref int shelterId)
        {
            float nextThreshold = Balance.ShelterSpacing;
            for (int k = 0; k < actualBase; k++)
            {
                float dist = (k + .5f) * Balance.TileSize;
                if (dist < nextThreshold) continue;
                layout.shelters.Add(new ShelterLayout { id = shelterId++, position = steps[k].pos, radius = Balance.ShelterRadius, branchId = branch.id });
                nextThreshold += Balance.ShelterSpacing;
            }
        }

        static void AddAltars(MapLayout layout, BranchLayout branch, List<Step> steps, int actualBase, int seed, ref int altarId)
        {
            var rng = SeededRng.Stream(seed, "map.branch." + branch.id + ".altars");
            int count = rng.Chance(.5f) ? 2 : 1;
            for (int i = 0; i < count && actualBase > 4; i++)
            {
                int idx = rng.Next(4, actualBase - 1);
                layout.altars.Add(new AltarLayout { id = altarId++, position = steps[idx].pos, branchId = branch.id });
            }
        }

        static void AddShortcuts(List<TileLayout> mainTiles, int branchId, SeededRng rng, List<GapLayout> gaps, ref int gapId)
        {
            for (int i = 0; i < mainTiles.Count - 4; i++)
                for (int j = i + 4; j < mainTiles.Count; j++)
                {
                    float d = XZDist(mainTiles[i].position, mainTiles[j].position);
                    if (d > .5f && d <= Balance.GapMaxLength && rng.Chance(Balance.ShortcutChance))
                    {
                        mainTiles[i].shortcut = true;
                        mainTiles[j].shortcut = true;
                        gaps.Add(new GapLayout { id = gapId++, from = mainTiles[i].position, to = mainTiles[j].position, branchId = branchId });
                        return; // ponytail: one shortcut per branch is enough texture, not an exhaustive search
                    }
                }
        }

        static Vector3 BranchEndPosition(MapLayout layout, int branchId)
        {
            foreach (var p in layout.prisons)
                if (p.branchId == branchId && p.branchEnd) return p.position;
            foreach (var b in layout.branches)
                if (b.id == branchId) return b.anchor;
            return Vector3.zero;
        }

        static void TryConnect(MapLayout layout, HashSet<(int, int)> occupied, Vector3 hubCenter, int branchA, int branchB, ref int tileIndex, ref int connectionId)
        {
            Vector3 endA = BranchEndPosition(layout, branchA);
            Vector3 endB = BranchEndPosition(layout, branchB);
            var start = CellOf(endA, hubCenter);
            var goal = CellOf(endB, hubCenter);
            if (start.Equals(goal)) return;

            var prev = new Dictionary<(int, int), (int, int)>();
            var visited = new HashSet<(int, int)> { start };
            var queue = new Queue<(int, int)>();
            queue.Enqueue(start);
            var dirs = new (int, int)[] { (1, 0), (-1, 0), (0, 1), (0, -1) };
            bool found = false;
            int budget = 8000;
            while (queue.Count > 0 && budget-- > 0)
            {
                var cur = queue.Dequeue();
                if (cur.Equals(goal)) { found = true; break; }
                foreach (var d in dirs)
                {
                    var next = (cur.Item1 + d.Item1, cur.Item2 + d.Item2);
                    if (visited.Contains(next)) continue;
                    bool isGoal = next.Equals(goal);
                    if (!isGoal && occupied.Contains(next)) continue;
                    visited.Add(next);
                    prev[next] = cur;
                    queue.Enqueue(next);
                }
            }
            if (!found) return; // ponytail: give up on this pair, the other two usually succeed

            var cellPath = new List<(int, int)>();
            var c = goal;
            while (!c.Equals(start)) { cellPath.Add(c); c = prev[c]; }
            cellPath.Reverse();

            float baseDistance = PathDistanceAt(layout, branchA, endA);
            var tileIds = new List<int>();
            int step = 0;
            foreach (var cell in cellPath)
            {
                Vector3 pos = hubCenter + new Vector3(cell.Item1 * Balance.TileSize, 0, cell.Item2 * Balance.TileSize);
                pos.y = endA.y;
                occupied.Add(cell);
                step++;
                var tile = new TileLayout
                {
                    index = tileIndex++,
                    position = pos,
                    branchId = -1,
                    pathDistance = baseDistance + step * Balance.TileSize,
                    requiresUnlock = UnlockIds.Raiz2
                };
                layout.tiles.Add(tile);
                tileIds.Add(tile.index);
            }
            layout.connections.Add(new ConnectionLayout { id = connectionId++, fromBranch = branchA, toBranch = branchB, tiles = tileIds, requiresUnlock = UnlockIds.Raiz2 });
        }
    }
}
