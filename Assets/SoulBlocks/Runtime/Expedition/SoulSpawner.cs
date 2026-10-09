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
        readonly HashSet<Vector3> platformCenters = new HashSet<Vector3>();
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
            platformCenters.Clear();
            foreach (var tile in layout.tiles)
                if (tile.shortcut) platformCenters.Add(tile.position);

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
            float tileSize = platformCenters.Contains(spawn.position) ? Balance.PlatformSize : Balance.TileSize;
            for (int i = 0; i < count; i++)
            {
                var pickup = SoulPickup.Spawn(root, RandomPointOnTile(spawn.position, tileSize), (int)Balance.SoulValue);
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

        // Uniform over the spawn's own tile square (tiles are full squares, so every point has
        // ground). No physics query: tiles unlocked this same frame may not be active yet.
        Vector3 RandomPointOnTile(Vector3 center, float tileSize)
        {
            float half = tileSize * .45f;
            return center + new Vector3(scatterRng.Range(-half, half), 0f, scatterRng.Range(-half, half));
        }

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
