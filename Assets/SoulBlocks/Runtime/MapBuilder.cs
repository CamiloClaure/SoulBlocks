using System.Collections.Generic;
using UnityEngine;

namespace SoulBlocks
{
    // One curved ribbon mesh per TileLayout (its PathShape stretch; layer 8 so
    // SoulPlayer.Supported sees it), a disc per dash platform, gap-end posts, and shelter
    // placeholders with their ShelterZone. Prisons, souls, altars and portals are built
    // by the crews that own those features from this same MapLayout.
    public sealed class MapBuilder : MonoBehaviour
    {
        MapLayout layout;
        RunState run;
        Transform mapRoot;
        readonly Dictionary<int, GameObject> tileObjects = new Dictionary<int, GameObject>();

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

        public GameObject TileObject(int index) => tileObjects.TryGetValue(index, out var go) ? go : null;

        void OnUnlockGranted(string id)
        {
            if (layout == null) return;
            foreach (var tile in layout.tiles)
                if (tile.requiresUnlock == id && tileObjects.TryGetValue(tile.index, out var go))
                    go.SetActive(true);
        }
    }
}
