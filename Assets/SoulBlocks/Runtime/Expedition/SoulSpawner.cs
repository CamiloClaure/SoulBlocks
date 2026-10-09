using System.Collections.Generic;
using UnityEngine;

namespace SoulBlocks
{
    // Spawns souls for the layout's spawn points and for loose souls released elsewhere
    // (prisons, altars). Spawn-point souls are refreshed whenever an expedition ends; ground
    // souls persist across expeditions and saves via Run.groundSouls.
    public sealed class SoulSpawner : MonoBehaviour
    {
        MapLayout layout;
        RunState run;
        Transform root;
        readonly List<SoulPickup> spawnPointSouls = new List<SoulPickup>();
        readonly HashSet<int> spawnedSpawnPointIds = new HashSet<int>();
        readonly Dictionary<int, TileLayout> tilesByIndex = new Dictionary<int, TileLayout>();
        SeededRng scatterRng;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() => GameRoot.Register<SoulSpawner>();

        void OnEnable()
        {
            GameEvents.MapBuilt += OnMapBuilt;
            GameEvents.UnlockGranted += OnUnlockGranted;
            GameEvents.SoulsReleased += OnSoulsReleased;
            GameEvents.ExpeditionEnded += OnExpeditionEnded;
        }

        void OnDisable()
        {
            GameEvents.MapBuilt -= OnMapBuilt;
            GameEvents.UnlockGranted -= OnUnlockGranted;
            GameEvents.SoulsReleased -= OnSoulsReleased;
            GameEvents.ExpeditionEnded -= OnExpeditionEnded;
        }

        void OnMapBuilt(MapLayout mapLayout)
        {
            layout = mapLayout;
            run = GameRoot.Instance.Run;
            root = new GameObject("Generated Souls").transform;
            root.SetParent(transform, false);
            spawnPointSouls.Clear();
            spawnedSpawnPointIds.Clear();
            scatterRng = NewScatterRng();
            tilesByIndex.Clear();
            foreach (var tile in layout.tiles) tilesByIndex[tile.index] = tile;

            foreach (var spawn in layout.soulSpawns)
                if (string.IsNullOrEmpty(spawn.requiresUnlock) || run.HasUnlock(spawn.requiresUnlock))
                    SpawnPoint(spawn);

            // Loose souls left on the ground from a previous session (save/load).
            foreach (var ground in run.groundSouls.ToArray())
                SpawnGround(ground);
        }

        void OnUnlockGranted(string id)
        {
            if (layout == null) return;
            foreach (var spawn in layout.soulSpawns)
                if (spawn.requiresUnlock == id && !spawnedSpawnPointIds.Contains(spawn.id))
                    SpawnPoint(spawn);
        }

        void OnSoulsReleased(Vector3 position, int count)
        {
            if (run == null) return;
            for (int i = 0; i < count; i++)
            {
                var ground = new GroundSoul { position = ScatterPoint(position, i, count), value = (int)Balance.SoulValue };
                run.groundSouls.Add(ground);
                SpawnGround(ground);
            }
        }

        // Old uncollected spawn-point souls are replaced for the next expedition; ground souls
        // (released from prisons/altars) persist and are not touched here.
        void OnExpeditionEnded(ExpeditionEndReason reason)
        {
            foreach (var pickup in spawnPointSouls)
                if (pickup != null) Destroy(pickup.gameObject);
            spawnPointSouls.Clear();
            spawnedSpawnPointIds.Clear();

            if (layout == null || run == null) return;
            scatterRng = NewScatterRng();
            foreach (var spawn in layout.soulSpawns)
                if (string.IsNullOrEmpty(spawn.requiresUnlock) || run.HasUnlock(spawn.requiresUnlock))
                    SpawnPoint(spawn);
        }

        void SpawnPoint(SoulSpawnLayout spawn)
        {
            spawnedSpawnPointIds.Add(spawn.id);
            int count = ExpeditionController.SpawnCountFor(spawn);
            tilesByIndex.TryGetValue(spawn.tileIndex, out var tile);
            for (int i = 0; i < count; i++)
            {
                var pickup = SoulPickup.Spawn(root, tile != null ? PathShape.RandomPoint(tile, scatterRng, .5f) : spawn.position, (int)Balance.SoulValue);
                spawnPointSouls.Add(pickup);
            }
        }

        void SpawnGround(GroundSoul ground)
        {
            var pickup = SoulPickup.Spawn(root, ground.position, ground.value);
            pickup.OnCollected += () => run.groundSouls.Remove(ground);
        }

        // New positions every expedition, same ones for the same seed and expedition.
        SeededRng NewScatterRng() => SeededRng.Stream(run.effectiveSeed, "souls.scatter." + run.expeditionIndex);

        static Vector3 ScatterPoint(Vector3 center, int index, int count)
        {
            float angle = index * Mathf.PI * 2f / Mathf.Max(1, count);
            for (float radius = .5f; radius <= Balance.GroundSoulScatterRadius; radius += .5f)
            {
                Vector3 candidate = center + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                if (SoulPlayer.IsSupportedAt(candidate)) return candidate;
            }
            return center; // ponytail: no supported point found in the scatter radius, drop it at the source
        }
    }
}
