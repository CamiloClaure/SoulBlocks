using UnityEngine;

namespace SoulBlocks
{
    // Marks the player's current tile discovered the first time they stand on it (the only
    // writer of Run.discoveredTiles).
    public sealed class TileDiscovery : MonoBehaviour
    {
        MapLayout layout;
        RunState run;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() => GameRoot.Register<TileDiscovery>();

        void OnEnable() { GameEvents.MapBuilt += OnMapBuilt; }
        void OnDisable() { GameEvents.MapBuilt -= OnMapBuilt; }

        void OnMapBuilt(MapLayout mapLayout)
        {
            layout = mapLayout;
            run = GameRoot.Instance.Run;
        }

        // ponytail: linear scan over every tile each frame; fine for a few hundred tiles, swap
        // for a grid lookup if the map grows a lot.
        void Update()
        {
            if (layout == null || run == null) return;
            var root = GameRoot.Instance;
            if (root == null || root.Player == null) return;
            Vector3 pos = root.Player.transform.position;

            float best = float.MaxValue;
            int bestIndex = -1;
            foreach (var tile in layout.tiles)
            {
                float dx = tile.position.x - pos.x, dz = tile.position.z - pos.z;
                float d2 = dx * dx + dz * dz;
                if (d2 < best) { best = d2; bestIndex = tile.index; }
            }
            float halfTile = Balance.TileSize * .5f;
            if (bestIndex < 0 || best > halfTile * halfTile) return;
            if (run.discoveredTiles.Contains(bestIndex)) return;
            run.discoveredTiles.Add(bestIndex);
            GameEvents.RaiseTileDiscovered(bestIndex);
        }
    }
}
