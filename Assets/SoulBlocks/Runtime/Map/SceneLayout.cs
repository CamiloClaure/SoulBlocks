using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SoulBlocks
{
    // Builds the run's MapLayout from what is placed in the scene, the hand-built replacement
    // for the old seeded generator. Every Branch AuthoredPath is an expedition branch (a Branch
    // path nested under another one extends it) and every Connection path a bridge between the
    // two branches nearest its ends; both are cut into Balance.TileSize tiles along the curve.
    // Prison, shelter, portal and risk altar sites become the layout entries, on the branch of
    // the path they sit under (or the nearest branch tile). Ids follow hierarchy order, so they
    // stay stable while nothing is reordered: saved prison damage and discovered tiles use them.
    public static class SceneLayout
    {
        const int CurveSamples = 400;
        const int EdgeSamplesPerTile = 6;

        public static MapLayout FromScene(Scene scene, Vector3 hubCenter)
        {
            var layout = new MapLayout { hubCenter = hubCenter };
            var branchOf = new Dictionary<AuthoredPath, BranchLayout>();

            foreach (var path in Collect<AuthoredPath>(scene))
            {
                if (path.role != PathRole.Branch || path.points == null || path.points.Count < 2) continue;
                var owner = OutermostBranch(path);
                if (!branchOf.TryGetValue(owner, out var branch))
                {
                    branch = new BranchLayout
                    {
                        id = layout.branches.Count,
                        specialty = owner.specialty,
                        anchor = owner.transform.TransformPoint(owner.Evaluate(0f)),
                        direction = Flat(owner.transform.TransformPoint(owner.Evaluate(.05f)) - owner.transform.TransformPoint(owner.Evaluate(0f))).normalized
                    };
                    layout.branches.Add(branch);
                    branchOf[owner] = branch;
                }
                branchOf[path] = branch;
                // An extension continues from the far end of what the branch has walked so far.
                float start = path == owner ? 0f : branch.length;
                branch.length = start + AddTiles(layout, path, branch.id, start, UnlockGate.RequiredFor(path.transform), null);
            }

            foreach (var path in Collect<AuthoredPath>(scene))
            {
                if (path.role != PathRole.Connection || path.points == null || path.points.Count < 2) continue;
                var connection = new ConnectionLayout { id = layout.connections.Count, requiresUnlock = UnlockGate.RequiredFor(path.transform) };
                AddTiles(layout, path, -1, 0f, connection.requiresUnlock, connection.tiles);
                connection.fromBranch = NearestBranch(layout, path.transform.TransformPoint(path.Evaluate(0f)));
                connection.toBranch = NearestBranch(layout, path.transform.TransformPoint(path.Evaluate(1f)));
                layout.connections.Add(connection);
            }

            foreach (var site in Collect<PrisonSite>(scene))
            {
                var prison = new PrisonLayout
                {
                    id = layout.prisons.Count,
                    position = site.transform.position,
                    tier = site.tier,
                    props = site.props,
                    groupId = site.groupId,
                    pattern = site.pattern,
                    branchId = BranchOf(layout, branchOf, site.transform),
                    branchEnd = site.branchEnd,
                    requiresUnlock = UnlockGate.RequiredFor(site.transform)
                };
                layout.prisons.Add(prison);
                if (prison.branchEnd && prison.branchId >= 0) layout.branches[prison.branchId].endPrisonId = prison.id;
            }

            foreach (var site in Collect<ShelterSite>(scene))
            {
                site.Id = layout.shelters.Count;
                layout.shelters.Add(new ShelterLayout { id = site.Id, position = site.transform.position, radius = site.radius, branchId = BranchOf(layout, branchOf, site.transform) });
            }

            foreach (var site in Collect<PortalSite>(scene))
                layout.portals.Add(new PortalLayout { id = layout.portals.Count, position = site.transform.position, branchId = BranchOf(layout, branchOf, site.transform) });

            foreach (var site in Collect<RiskAltarSite>(scene))
                layout.altars.Add(new AltarLayout { id = layout.altars.Count, position = site.transform.position, branchId = BranchOf(layout, branchOf, site.transform) });

            return layout;
        }

        // Inactive objects included: gated content is part of the layout, just locked.
        static List<T> Collect<T>(Scene scene) where T : Component
        {
            var found = new List<T>();
            foreach (var root in scene.GetRootGameObjects()) found.AddRange(root.GetComponentsInChildren<T>(true));
            return found;
        }

        static AuthoredPath OutermostBranch(AuthoredPath path)
        {
            var owner = path;
            for (var t = path.transform.parent; t != null; t = t.parent)
            {
                var outer = t.GetComponent<AuthoredPath>();
                if (outer != null && outer.role == PathRole.Branch) owner = outer;
            }
            return owner;
        }

        // The branch of the nearest AuthoredPath above `site` when that path is a branch, else
        // the branch of the nearest branch tile (sites on platforms, decor paths, the island).
        static int BranchOf(MapLayout layout, Dictionary<AuthoredPath, BranchLayout> branchOf, Transform site)
        {
            var path = site.GetComponentInParent<AuthoredPath>(true);
            if (path != null && branchOf.TryGetValue(path, out var branch)) return branch.id;
            return NearestBranch(layout, site.position);
        }

        static int NearestBranch(MapLayout layout, Vector3 position)
        {
            int best = -1;
            float bestDistance = float.MaxValue;
            foreach (var tile in layout.tiles)
            {
                if (tile.branchId < 0) continue;
                float distance = Flat(tile.position - position).sqrMagnitude;
                if (distance < bestDistance) { bestDistance = distance; best = tile.branchId; }
            }
            return best;
        }

        // Cuts the path into equal stretches about TileSize long. Each tile's edges follow the
        // ribbon (center +- half width) so MapScreen draws the real curve. Returns the length.
        static float AddTiles(MapLayout layout, AuthoredPath path, int branchId, float startDistance, string unlock, List<int> indices)
        {
            var points = new List<Vector3>(CurveSamples + 1);
            var distances = new List<float>(CurveSamples + 1);
            float total = 0f;
            for (int i = 0; i <= CurveSamples; i++)
            {
                var point = path.transform.TransformPoint(path.Evaluate(i / (float)CurveSamples));
                if (i > 0) total += Vector3.Distance(points[i - 1], point);
                points.Add(point);
                distances.Add(total);
            }

            float halfWidth = path.width * Mathf.Abs(path.transform.lossyScale.x) * .5f;
            int count = Mathf.Max(1, Mathf.RoundToInt(total / Balance.TileSize));
            float step = total / count;
            for (int k = 0; k < count; k++)
            {
                var tile = new TileLayout
                {
                    index = layout.tiles.Count,
                    position = PointAt(points, distances, (k + .5f) * step),
                    branchId = branchId,
                    pathDistance = startDistance + (k + .5f) * step,
                    requiresUnlock = unlock
                };
                for (int s = 0; s <= EdgeSamplesPerTile; s++)
                {
                    float d = (k + s / (float)EdgeSamplesPerTile) * step;
                    Vector3 center = PointAt(points, distances, d);
                    Vector3 tangent = Flat(PointAt(points, distances, d + .5f) - PointAt(points, distances, d - .5f)).normalized;
                    Vector3 right = Vector3.Cross(Vector3.up, tangent) * halfWidth;
                    tile.edgeLeft.Add(center - right);
                    tile.edgeRight.Add(center + right);
                }
                layout.tiles.Add(tile);
                indices?.Add(tile.index);
            }
            return total;
        }

        static Vector3 PointAt(List<Vector3> points, List<float> distances, float distance)
        {
            distance = Mathf.Clamp(distance, 0f, distances[distances.Count - 1]);
            for (int i = 1; i < distances.Count; i++)
            {
                if (distances[i] < distance) continue;
                float span = distances[i] - distances[i - 1];
                return Vector3.Lerp(points[i - 1], points[i], span > 0f ? (distance - distances[i - 1]) / span : 0f);
            }
            return points[points.Count - 1];
        }

        static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }
    }
}
