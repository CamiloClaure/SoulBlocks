using System.Collections.Generic;
using UnityEngine;

namespace SoulBlocks
{
    // Tiles only: one cube per TileLayout, layer 8 so SoulPlayer.Supported sees it. Shelters,
    // gaps, altars, portals etc. belong to the crews that own those features.
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
                var go = Placeholder.Make(PrimitiveType.Cube, new Color(.05f, .05f, .07f), "Tile " + tile.index,
                    mapRoot, center, new Vector3(Balance.TileSize, Balance.TileThickness, Balance.TileSize), true);
                go.layer = 8;
                bool locked = !string.IsNullOrEmpty(tile.requiresUnlock) && !run.HasUnlock(tile.requiresUnlock);
                go.SetActive(!locked);
                tileObjects[tile.index] = go;
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
