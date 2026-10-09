using System.Collections.Generic;
using UnityEngine;

namespace SoulBlocks
{
    // One cube per TileLayout (layer 8 so SoulPlayer.Supported sees it), gap-end posts, and
    // shelter placeholders with their ShelterZone. Prisons, souls, altars and portals are built
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

            foreach (var tile in layout.tiles)
            {
                Vector3 center = tile.position - new Vector3(0, Balance.TileThickness * .5f, 0);
                float size = tile.shortcut ? Balance.PlatformSize : Balance.TileSize; // shortcut = dash-only side platform
                var go = Placeholder.Make(PrimitiveType.Cube, new Color(.05f, .05f, .07f), "Tile " + tile.index,
                    mapRoot, center, new Vector3(size, Balance.TileThickness, size), true);
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
                var go = Placeholder.Make(PrimitiveType.Cylinder, new Color(.55f, .95f, 1f, .5f), "Shelter " + shelter.id,
                    mapRoot, shelter.position + Vector3.up * .15f, new Vector3(diameter, .15f, diameter), false, .3f);
                var zone = go.AddComponent<ShelterZone>();
                zone.ShelterId = shelter.id;
                zone.Radius = shelter.radius;
            }
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
