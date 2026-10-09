using System.Collections.Generic;
using UnityEngine;

namespace SoulBlocks
{
    public static partial class MapGen
    {
        // Last pass of TryGenerate, after the grid work (walks, occupancy, A* connections) is
        // final: jitter tile centers off the grid, carry everything placed on a tile along,
        // give each tile a width, build the curved ribbon (PathShape), then re-seat the dash
        // platforms against the real ribbon edge and scatter prisons across the ribbon.
        static void Naturalize(MapLayout layout, int seed)
        {
            var oldPosition = new Dictionary<int, Vector3>();
            var byIndex = new Dictionary<int, TileLayout>();
            var jitter = SeededRng.Stream(seed, "map.jitter");
            var widths = SeededRng.Stream(seed, "map.width");
            var halfWidth = new Dictionary<int, float>();
            foreach (var tile in layout.tiles)
            {
                byIndex[tile.index] = tile;
                if (tile.shortcut) continue;
                oldPosition[tile.index] = tile.position;
                halfWidth[tile.index] = Balance.PathHalfWidth * (1f + widths.Range(-Balance.PathWidthNoise, Balance.PathWidthNoise));
                // The first three tiles of a branch stay on the grid, straight and at full width:
                // the grid snap can put the path up to TileSize/2 sideways off the island anchor,
                // and the widest ribbon still covers the anchor so the island edge always leads on.
                if (tile.branchId >= 0 && tile.pathDistance < 3f * Balance.TileSize - .01f)
                {
                    halfWidth[tile.index] = Balance.PathHalfWidth * (1f + Balance.PathWidthNoise);
                    continue;
                }
                tile.position += new Vector3(jitter.Range(-Balance.PathJitter, Balance.PathJitter), 0f, jitter.Range(-Balance.PathJitter, Balance.PathJitter));
            }

            // Everything placed on a tile moves with it (looked up by its pre-jitter position).
            foreach (var prison in layout.prisons) prison.position += DeltaOfNearest(layout, oldPosition, prison.branchId, prison.position);
            foreach (var shelter in layout.shelters) shelter.position += DeltaOfNearest(layout, oldPosition, shelter.branchId, shelter.position);
            foreach (var altar in layout.altars) altar.position += DeltaOfNearest(layout, oldPosition, altar.branchId, altar.position);
            foreach (var portal in layout.portals) portal.position += DeltaOfNearest(layout, oldPosition, portal.branchId, portal.position);
            foreach (var spawn in layout.soulSpawns)
                if (byIndex.TryGetValue(spawn.tileIndex, out var tile) && !tile.shortcut) spawn.position = tile.position;

            foreach (var branch in layout.branches) BuildBranchRibbon(layout, branch, halfWidth);
            foreach (var connection in layout.connections) BuildConnectionRibbon(layout, connection, byIndex, halfWidth);

            ReseatPlatforms(layout, oldPosition, byIndex);
            ScatterPrisons(layout, SeededRng.Stream(seed, "map.prisonOffsets"));
        }

        static Vector3 DeltaOfNearest(MapLayout layout, Dictionary<int, Vector3> oldPosition, int branchId, Vector3 position)
        {
            var tile = NearestTile(layout, branchId, position, oldPosition);
            return tile != null ? tile.position - oldPosition[tile.index] : Vector3.zero;
        }

        // Nearest non-platform tile of the branch, measured against `positions` (pre-jitter
        // positions when given, current ones otherwise).
        static TileLayout NearestTile(MapLayout layout, int branchId, Vector3 position, Dictionary<int, Vector3> positions = null)
        {
            TileLayout best = null;
            float bestD = float.MaxValue;
            foreach (var tile in layout.tiles)
            {
                if (tile.shortcut || tile.branchId != branchId) continue;
                Vector3 p = positions != null ? positions[tile.index] : tile.position;
                float dx = p.x - position.x, dz = p.z - position.z;
                float d = dx * dx + dz * dz;
                if (d < bestD) { bestD = d; best = tile; }
            }
            return best;
        }

        // [a tile back into the island, tiles by walking distance, a tile past the end]: the first
        // and last pieces then start/end half a tile out, like the old square tiles did.
        static void BuildBranchRibbon(MapLayout layout, BranchLayout branch, Dictionary<int, float> halfWidth)
        {
            var tiles = new List<TileLayout>();
            foreach (var tile in layout.tiles)
                if (!tile.shortcut && tile.branchId == branch.id) tiles.Add(tile);
            if (tiles.Count == 0) return;
            tiles.Sort((x, y) => x.pathDistance.CompareTo(y.pathDistance));

            Vector3 startDir = tiles.Count > 1 ? (tiles[1].position - tiles[0].position).normalized : branch.direction;
            Vector3 endDir = tiles.Count > 1 ? (tiles[tiles.Count - 1].position - tiles[tiles.Count - 2].position).normalized : branch.direction;

            var points = new List<Vector3> { tiles[0].position - startDir * Balance.TileSize };
            var widths = new List<float> { halfWidth[tiles[0].index] };
            var owners = new List<TileLayout> { null };
            foreach (var tile in tiles)
            {
                points.Add(tile.position);
                widths.Add(halfWidth[tile.index]);
                owners.Add(tile);
            }
            var last = tiles[tiles.Count - 1];
            points.Add(last.position + endDir * Balance.TileSize);
            widths.Add(halfWidth[last.index]);
            owners.Add(null);
            PathShape.BuildPieces(points, widths, owners);
        }

        // [end of branch A, the connection's own tiles, end of branch B]: the ends belong to the
        // branches, so the bridge's first and last stretch run all the way into them.
        static void BuildConnectionRibbon(MapLayout layout, ConnectionLayout connection, Dictionary<int, TileLayout> byIndex, Dictionary<int, float> halfWidth)
        {
            var points = new List<Vector3> { BranchEndPosition(layout, connection.fromBranch) };
            var widths = new List<float> { Balance.PathHalfWidth };
            var owners = new List<TileLayout> { null };
            foreach (int index in connection.tiles)
            {
                if (!byIndex.TryGetValue(index, out var tile)) continue;
                bool own = tile.branchId < 0;
                points.Add(tile.position);
                widths.Add(halfWidth.TryGetValue(tile.index, out float w) ? w : Balance.PathHalfWidth);
                owners.Add(own ? tile : null);
            }
            if (owners[owners.Count - 1] != null)
            {
                points.Add(BranchEndPosition(layout, connection.toBranch));
                widths.Add(Balance.PathHalfWidth);
                owners.Add(null);
            }
            PathShape.BuildPieces(points, widths, owners, extendToForeignEnds: true);
        }

        // A platform sits one GapMaxLength off the ribbon's real edge on its side, measured from
        // the jittered anchor tile; its gap and its soul spawn follow.
        static void ReseatPlatforms(MapLayout layout, Dictionary<int, Vector3> oldPosition, Dictionary<int, TileLayout> byIndex)
        {
            foreach (var gap in layout.gaps)
            {
                Vector3 side = gap.to - gap.from;
                side.y = 0f;
                side.Normalize();
                var anchor = NearestTile(layout, gap.branchId, gap.from, oldPosition);
                TileLayout platform = null;
                float bestD = float.MaxValue;
                foreach (var tile in layout.tiles)
                {
                    if (!tile.shortcut) continue;
                    float d = (tile.position - gap.to).sqrMagnitude;
                    if (d < bestD) { bestD = d; platform = tile; }
                }
                if (anchor == null || platform == null) continue;

                float edge = OutermostEdge(layout, anchor.position, side, Balance.PlatformSize * .5f + 2f);
                gap.from = anchor.position + side * edge;
                gap.to = gap.from + side * Balance.GapMaxLength;
                platform.position = gap.to + side * (Balance.PlatformSize * .5f);
                foreach (var spawn in layout.soulSpawns)
                    if (spawn.tileIndex == platform.index) spawn.position = platform.position;
            }
        }

        // How far the ribbon reaches along `side` from `origin`, over every ribbon sample within
        // `window` meters along the perpendicular: the platform must clear the widest bulge
        // beside it, not just the edge at the anchor tile's center.
        static float OutermostEdge(MapLayout layout, Vector3 origin, Vector3 side, float window)
        {
            Vector3 along = new Vector3(-side.z, 0f, side.x);
            float edge = 0f;
            foreach (var tile in layout.tiles)
                for (int k = 0; k < tile.edgeLeft.Count; k++)
                {
                    edge = Reach(tile.edgeLeft[k], origin, side, along, window, edge);
                    edge = Reach(tile.edgeRight[k], origin, side, along, window, edge);
                }
            return edge;
        }

        static float Reach(Vector3 point, Vector3 origin, Vector3 side, Vector3 along, float window, float edge)
        {
            Vector3 d = point - origin;
            float lateral = d.x * side.x + d.z * side.z;
            if (lateral < 0f || lateral > Balance.TileSize) return edge; // other side, or another path further out
            return Mathf.Abs(d.x * along.x + d.z * along.z) <= window ? Mathf.Max(edge, lateral) : edge;
        }

        // Prisons anywhere across the ribbon width at their tile; a group moves as one block.
        // Branch-end prisons (they double as portals) and the Nucleo stay centered.
        static void ScatterPrisons(MapLayout layout, SeededRng rng)
        {
            var groupOffset = new Dictionary<int, Vector3>();
            foreach (var prison in layout.prisons)
            {
                if (prison.branchEnd || prison.tier == PrisonTier.Nucleo) continue;
                if (prison.groupId >= 0 && groupOffset.TryGetValue(prison.groupId, out var shared))
                {
                    prison.position += shared;
                    continue;
                }
                var tile = NearestTile(layout, prison.branchId, prison.position);
                if (tile == null) continue;
                PathShape.CenterSpan(tile, out var left, out var right);
                float margin = prison.groupId >= 0 ? Balance.PrisonGroupSpacing + 1f : 1f;
                Vector3 offset = PathShape.Across(left, right, margin, rng) - (left + right) * .5f;
                offset.y = 0f;
                prison.position += offset;
                if (prison.groupId >= 0) groupOffset[prison.groupId] = offset;
            }

            // A group's pattern is laid out on the grid heading, so on a bend or a narrow stretch
            // one of its prisons can end up past the edge: pull those back onto the ribbon.
            foreach (var prison in layout.prisons) prison.position = OntoRibbon(layout, prison.position, 1f);
        }

        // `position` if it lies within `margin` of a ribbon cross-section, else the closest point
        // on the nearest cross-section (kept `margin` in from both edges).
        static Vector3 OntoRibbon(MapLayout layout, Vector3 position, float margin)
        {
            float best = float.MaxValue;
            Vector3 closest = position;
            foreach (var tile in layout.tiles)
                for (int k = 0; k < tile.edgeLeft.Count; k++)
                {
                    Vector3 a = tile.edgeLeft[k], b = tile.edgeRight[k];
                    Vector3 ab = b - a;
                    float length = Mathf.Sqrt(ab.x * ab.x + ab.z * ab.z);
                    float inset = length > 2f * margin ? margin / length : .5f;
                    float t = Mathf.Clamp(((position.x - a.x) * ab.x + (position.z - a.z) * ab.z) / Mathf.Max(1e-6f, length * length), inset, 1f - inset);
                    Vector3 point = a + ab * t;
                    float dx = point.x - position.x, dz = point.z - position.z;
                    float d = dx * dx + dz * dz;
                    if (d < best) { best = d; closest = point; }
                }
            if (best <= margin * margin) return position;
            closest.y = position.y;
            return closest;
        }
    }
}
