using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SoulBlocks
{
    // One curved ribbon mesh per TileLayout (its PathShape stretch; layer 8 so
    // SoulPlayer.Supported sees it) under a rock cave shell, a disc per dash platform, gap-end
    // posts, and shelter placeholders with their ShelterZone. Prisons, souls, altars and portals are built
    // by the crews that own those features from this same MapLayout.
    public sealed class MapBuilder : MonoBehaviour
    {
        MapLayout layout;
        RunState run;
        Transform mapRoot;
        readonly Dictionary<int, GameObject> tileObjects = new Dictionary<int, GameObject>();
        readonly List<GameObject> caves = new List<GameObject>();

        void OnEnable() { GameEvents.UnlockGranted += OnUnlockGranted; }
        void OnDisable() { GameEvents.UnlockGranted -= OnUnlockGranted; }

        public void Build(MapLayout mapLayout, RunState runState)
        {
            layout = mapLayout;
            run = runState;
            if (mapRoot != null) Destroy(mapRoot.gameObject);
            tileObjects.Clear();

            mapRoot = new GameObject("Generated Map").transform;
            mapRoot.SetParent(transform, false);
            mapRoot.position = Vector3.zero; // ribbon meshes are built in world space

            foreach (var tile in layout.tiles)
            {
                GameObject go;
                if (tile.shortcut)
                {
                    // Dash-only side platform: a round disc, its capsule collider swapped for the mesh.
                    go = Placeholder.Make(PrimitiveType.Cylinder, PathColor, "Platform " + tile.index, mapRoot,
                        tile.position - new Vector3(0, Balance.TileThickness * .5f, 0),
                        new Vector3(Balance.PlatformSize, Balance.TileThickness * .5f, Balance.PlatformSize));
                    go.AddComponent<MeshCollider>();
                }
                else go = Placeholder.MakeMesh(RibbonMesh(tile), PathColor, "Tile " + tile.index, mapRoot);
                go.layer = 8;
                bool locked = !string.IsNullOrEmpty(tile.requiresUnlock) && !run.HasUnlock(tile.requiresUnlock);
                go.SetActive(!locked);
                tileObjects[tile.index] = go;
            }

            BuildCaves();

            foreach (var gap in layout.gaps)
            {
                Placeholder.Make(PrimitiveType.Cylinder, new Color(1f, .6f, .1f), "Gap End",
                    mapRoot, gap.from + Vector3.up * .4f, new Vector3(.2f, .3f, .2f));
                Placeholder.Make(PrimitiveType.Cylinder, new Color(1f, .6f, .1f), "Gap End",
                    mapRoot, gap.to + Vector3.up * .4f, new Vector3(.2f, .3f, .2f));
            }

            foreach (var shelter in layout.shelters)
            {
                // ponytail: Placeholder's shared material is Opaque (frozen file, not ours to
                // change), so "translucent-looking" is approximated with a pale, slightly
                // emissive cyan rather than real alpha blending.
                float diameter = shelter.radius * 2f;
                var go = Placeholder.Make(PrimitiveType.Cylinder, new Color(.45f, .8f, .9f), "Shelter " + shelter.id,
                    mapRoot, shelter.position + Vector3.up * .03f, new Vector3(diameter, .02f, diameter), false, .15f);
                var zone = go.AddComponent<ShelterZone>();
                zone.ShelterId = shelter.id;
                zone.Radius = shelter.radius;
            }
        }

        static readonly Color PathColor = new Color(.05f, .05f, .07f);
        static readonly Color RockColor = new Color(.17f, .14f, .15f);

        // Slab under the ribbon edges: top face, both side walls and both end caps, flat-shaded
        // (every triangle gets its own vertices). World-space vertices; mapRoot sits at the origin.
        static Mesh RibbonMesh(TileLayout tile)
        {
            var vertices = new List<Vector3>();
            var down = Vector3.down * Balance.TileThickness;
            // 1 cm under the layout height: where the first stretch overlaps the island, the
            // island's own surface draws on top instead of flickering against the ribbon.
            var sink = Vector3.down * .01f;
            var L = tile.edgeLeft.ConvertAll(p => p + sink);
            var R = tile.edgeRight.ConvertAll(p => p + sink);
            void Tri(Vector3 a, Vector3 b, Vector3 c) { vertices.Add(a); vertices.Add(b); vertices.Add(c); }
            for (int k = 0; k + 1 < L.Count; k++)
            {
                Tri(L[k], L[k + 1], R[k]);
                Tri(R[k], L[k + 1], R[k + 1]);
                Tri(L[k], L[k] + down, L[k + 1]);
                Tri(L[k + 1], L[k] + down, L[k + 1] + down);
                Tri(R[k], R[k + 1], R[k] + down);
                Tri(R[k + 1], R[k + 1] + down, R[k] + down);
            }
            int n = L.Count - 1;
            Tri(L[0], R[0], L[0] + down);
            Tri(R[0], R[0] + down, L[0] + down);
            Tri(R[n], L[n], R[n] + down);
            Tri(L[n], L[n] + down, R[n] + down);

            var triangles = new int[vertices.Count];
            for (int i = 0; i < triangles.Length; i++) triangles[i] = i;
            var mesh = new Mesh { name = "Ribbon " + tile.index };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // Rock walls along both ribbon edges, end walls, and a ceiling between them, every face
        // turned inward: one-sided, so from outside a wall or above the ceiling the orbit camera
        // sees straight through (no camera collision needed). A wall stretch stays open where
        // another floor (next tile, bridge, platform, island) lies just beyond it, so tiles chain
        // into one tunnel and dead ends close; the ceiling stops over the island. Rock noise depends only on world position, so
        // neighboring tiles meet seamlessly. World space, flat-shaded, no collider.
        static Mesh CaveMesh(TileLayout tile, Func<Vector3, bool> floorBeyond, Func<Vector3, bool> onIsland)
        {
            var vertices = new List<Vector3>();
            float height = Balance.CaveHeight;
            float[] rows = { -Balance.TileThickness, height * .35f, height * .7f, height };
            var probes = new[] { .75f, 2.5f, Balance.GapMaxLength + 1f, Balance.GapMaxLength + 3f };

            void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 inward)
            {
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), inward) < 0f) { var t = b; b = c; c = t; }
                vertices.Add(a); vertices.Add(b); vertices.Add(c);
            }
            bool Open(Vector3 a, Vector3 b, Vector3 inward)
            {
                Vector3 mid = (a + b) * .5f;
                foreach (float d in probes) if (floorBeyond(mid - inward * d)) return true;
                return false;
            }
            // Bottom row sits on the ribbon edge; higher rows get rock noise and curl inward
            // (lean) so the walls vault into the ceiling.
            Vector3 Rock(Vector3 basePoint, Vector3 inward, float h, float lean)
            {
                float t = Mathf.Clamp01(h / height);
                float rock = (Mathf.PerlinNoise(basePoint.x * .23f + h * .41f, basePoint.z * .23f - h * .17f) - .5f) * 2.4f;
                return basePoint + inward * (lean * t * t + rock * Mathf.Clamp01(t * 4f)) + Vector3.up * h;
            }
            void Strip(List<Vector3> points, List<Vector3> inwards, float lean, float top)
            {
                for (int k = 0; k + 1 < points.Count; k++)
                {
                    Vector3 inward = (inwards[k] + inwards[k + 1]).normalized;
                    if (Open(points[k], points[k + 1], inward)) continue;
                    for (int r = 0; r + 1 < rows.Length; r++)
                    {
                        float h0 = rows[r], h1 = r + 2 == rows.Length ? top : rows[r + 1];
                        Vector3 a = Rock(points[k], inwards[k], h0, lean), b = Rock(points[k + 1], inwards[k + 1], h0, lean);
                        Vector3 c = Rock(points[k], inwards[k], h1, lean), d = Rock(points[k + 1], inwards[k + 1], h1, lean);
                        Tri(a, b, c, inward);
                        Tri(c, b, d, inward);
                    }
                }
            }

            var L = tile.edgeLeft;
            var R = tile.edgeRight;
            int n = L.Count;
            if (n < 2) return new Mesh { name = "Cave " + tile.index };
            const float Lean = 3f;
            var toRight = new List<Vector3>();
            var toLeft = new List<Vector3>();
            for (int k = 0; k < n; k++)
            {
                Vector3 across = R[k] - L[k];
                across.y = 0f;
                toRight.Add(across.normalized);
                toLeft.Add(-across.normalized);
            }
            Strip(L, toRight, Lean, height);
            Strip(R, toLeft, Lean, height);

            // End walls across the first and last cross-section, straight up past the ceiling's
            // highest bump so no sliver of sky shows between them.
            for (int end = 0; end < 2; end++)
            {
                int k = end == 0 ? 0 : n - 1, next = end == 0 ? 1 : n - 2;
                Vector3 inward = (L[next] + R[next] - L[k] - R[k]);
                inward.y = 0f;
                inward.Normalize();
                var points = new List<Vector3>();
                var inwards = new List<Vector3>();
                for (int i = 0; i <= 4; i++) { points.Add(Vector3.Lerp(L[k], R[k], i / 4f)); inwards.Add(inward); }
                Strip(points, inwards, 0f, height + 2f);
            }

            // Ceiling: the two wall tops plus a bumpy ridge down the middle; open sky over the island.
            for (int k = 0; k + 1 < n; k++)
            {
                if (onIsland((L[k] + R[k + 1]) * .5f)) continue;
                Vector3 l0 = Rock(L[k], toRight[k], height, Lean), l1 = Rock(L[k + 1], toRight[k + 1], height, Lean);
                Vector3 r0 = Rock(R[k], toLeft[k], height, Lean), r1 = Rock(R[k + 1], toLeft[k + 1], height, Lean);
                Vector3 m0 = Ridge(l0, r0), m1 = Ridge(l1, r1);
                Tri(l0, l1, m0, Vector3.down);
                Tri(m0, l1, m1, Vector3.down);
                Tri(m0, m1, r0, Vector3.down);
                Tri(r0, m1, r1, Vector3.down);
            }

            var triangles = new int[vertices.Count];
            for (int i = 0; i < triangles.Length; i++) triangles[i] = i;
            var mesh = new Mesh { name = "Cave " + tile.index };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static Vector3 Ridge(Vector3 left, Vector3 right)
        {
            Vector3 mid = (left + right) * .5f;
            mid.y += (Mathf.PerlinNoise(mid.x * .11f, mid.z * .11f) - .5f) * 3f;
            return mid;
        }

        public GameObject TileObject(int index) => tileObjects.TryGetValue(index, out var go) ? go : null;

        void OnUnlockGranted(string id)
        {
            if (layout == null) return;
            bool opened = false;
            foreach (var tile in layout.tiles)
                if (tile.requiresUnlock == id && tileObjects.TryGetValue(tile.index, out var go))
                {
                    go.SetActive(true);
                    opened = true;
                }
            if (opened) BuildCaves();
        }

        // A cave shell under every ribbon tile (child of the tile, so it hides with a locked one).
        // Walls open only toward floor that exists right now, so they never gape at a still-locked
        // bridge or deep stretch: unlocking tiles rebuilds every shell.
        void BuildCaves()
        {
            foreach (var cave in caves)
                if (cave != null) { Destroy(cave.GetComponent<MeshFilter>().sharedMesh); Destroy(cave); }
            caves.Clear();
            var island = GameRoot.Instance != null ? GameRoot.Instance.Island : null;
            bool OnIsland(Vector3 p) => island != null &&
                Physics.Raycast(p + Vector3.up * 5f, Vector3.down, out var hit, 10f, 1 << 8, QueryTriggerInteraction.Ignore) && hit.transform.IsChildOf(island);
            bool Locked(TileLayout t) => !string.IsNullOrEmpty(t.requiresUnlock) && !run.HasUnlock(t.requiresUnlock);

            foreach (var tile in layout.tiles)
            {
                if (tile.shortcut || !tileObjects.TryGetValue(tile.index, out var go)) continue;
                var mesh = CaveMesh(tile, p => OnIsland(p) || PathShape.OnAnyFloor(layout, p, tile, Locked), OnIsland);
                var cave = Placeholder.MakeMesh(mesh, RockColor, "Cave", go.transform, false);
                cave.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                caves.Add(cave);
            }
        }
    }
}
