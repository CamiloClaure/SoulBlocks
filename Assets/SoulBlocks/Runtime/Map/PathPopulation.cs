using System.Collections.Generic;
using UnityEngine;

namespace SoulBlocks
{
    // Fills the hand-built paths at runtime. Wandering souls line both sides of every active
    // AuthoredPath, with a few more in the void around them; yellow crystal groups sit on the
    // paths; and the souls a prison or crystal releases (GameEvents.SoulsReleased) appear where
    // it broke. Every expedition start refills the path souls at the current StatId.SoulDensity
    // (Abundancia): each spot keeps the soul wandering there and gets a new one if it was led
    // away or delivered. Nothing is ever destroyed here. Crystals are laid once.
    public sealed class PathPopulation : MonoBehaviour
    {
        public WanderingSpirit spiritPrefab;
        public BreakableCrystal crystalPrefab;
        public GuidingTorch torch;
        [Tooltip("Metros entre almas a densidad 1; la densidad (Abundancia) la acorta o alarga.")]
        [Min(4)] public float spiritSpacing = 6;
        [Range(1, 3)] public int spiritRows = 2;
        [Min(5)] public float crystalSpacing = 14;
        [Min(1)] public int maxSpirits = 240;
        [Min(1)] public int maxCrystals = 120;
        [Min(0)] public int freeSpiritCount = 64;
        public int SpiritCount => pathSpirits.Count;
        public int CrystalCount { get; private set; }

        struct Road { public AuthoredPath path; public List<Vector3> points; public float halfWidth; }
        readonly List<Road> roads = new List<Road>();
        List<WanderingSpirit> pathSpirits = new List<WanderingSpirit>();
        struct Spot { public Vector3 position; public string label; }
        Transform soulRoot, crystalRoot;
        bool crystalsLaid;
        int spawned;

        void OnEnable()
        {
            GameEvents.MapBuilt += OnMapBuilt;
            GameEvents.ExpeditionStarted += OnExpeditionStarted;
            GameEvents.SoulsReleased += OnSoulsReleased;
        }

        void OnDisable()
        {
            GameEvents.MapBuilt -= OnMapBuilt;
            GameEvents.ExpeditionStarted -= OnExpeditionStarted;
            GameEvents.SoulsReleased -= OnSoulsReleased;
        }

        void OnMapBuilt(MapLayout layout) => Populate();
        void OnExpeditionStarted(int index) => Populate();

        static float Density => GameRoot.Instance != null && GameRoot.Instance.Stats != null
            ? Mathf.Max(.05f, GameRoot.Instance.Stats.Get(StatId.SoulDensity)) : 1f;

        public bool InsideRoad(Vector3 position, float clearance = .5f)
        {
            foreach (var road in roads)
                foreach (var point in road.points)
                {
                    var delta = point - position;
                    delta.y = 0;
                    if (delta.sqrMagnitude < (road.halfWidth + clearance) * (road.halfWidth + clearance)) return true;
                }
            return false;
        }

        // Re-reads the active paths (gated ones open with their unlock), lays the crystals once
        // and refills every soul spot.
        public void Populate()
        {
            if (spiritPrefab == null || crystalPrefab == null || torch == null) return;
            if (soulRoot == null)
            {
                soulRoot = new GameObject("Path Souls").transform;
                soulRoot.SetParent(transform, false);
                crystalRoot = new GameObject("Path Crystal Groups").transform;
                crystalRoot.SetParent(transform, false);
            }
            ReadRoads();
            if (!crystalsLaid)
            {
                crystalsLaid = true;
                LayCrystals();
            }

            var spots = new List<Spot>();
            float density = Density;
            AddRoadSpots(spots, density);
            AddVoidSpots(spots, density);
            var free = pathSpirits.FindAll(s => s != null && !s.Following);
            var kept = pathSpirits.FindAll(s => s != null && s.Following);
            foreach (var spot in spots)
            {
                if (kept.Count >= maxSpirits) break;
                int nearest = -1;
                float best = 3f * 3f;
                for (int i = 0; i < free.Count; i++)
                {
                    float d = (free[i].Home - spot.position).sqrMagnitude;
                    if (d < best)
                    { best = d; nearest = i; }
                }
                if (nearest >= 0)
                {
                    kept.Add(free[nearest]);
                    free.RemoveAt(nearest);
                }
                else kept.Add(Spawn(spot.position, spot.label));
            }
            kept.AddRange(free);
            pathSpirits = kept;
        }

        void ReadRoads()
        {
            roads.Clear();
            foreach (var path in FindObjectsByType<AuthoredPath>(FindObjectsSortMode.InstanceID))
            {
                if (path.points == null || path.points.Count < 2) continue;
                path.Rebuild();
                var points = new List<Vector3>();
                for (int i = 0; i <= 1000; i++) points.Add(path.transform.TransformPoint(path.Evaluate(i / 1000f)));
                float scale = Mathf.Max(Mathf.Abs(path.transform.lossyScale.x), Mathf.Abs(path.transform.lossyScale.z));
                roads.Add(new Road { path = path, points = points, halfWidth = path.width * scale * .5f });
            }
            Physics.SyncTransforms();
        }

        // Both sides of every road, spiritRows deep, one station every spacing / density meters.
        void AddRoadSpots(List<Spot> spots, float density)
        {
            float spacing = Mathf.Max(4f, spiritSpacing) / density;
            foreach (var road in roads)
            {
                float distance = 0, next = 8;
                for (int i = 1; i < road.points.Count - 1; i++)
                {
                    distance += Vector3.Distance(road.points[i - 1], road.points[i]);
                    if (distance < next) continue;
                    next += spacing;
                    Vector3 forward = (road.points[i + 1] - road.points[i - 1]).normalized;
                    Vector3 side = Vector3.Cross(Vector3.up, forward).normalized;
                    for (int sign = -1; sign <= 1; sign += 2)
                        for (int row = 0; row < spiritRows; row++)
                        {
                            Vector3 point = road.points[i] + side * sign * (road.halfWidth + 3.5f + row * 3f) + forward * row * 1.5f;
                            if (InsideRoad(point, 2.2f)) continue;
                            if (Physics.Raycast(point + Vector3.up, Vector3.down, out var floor, 3, (1 << 8) | (1 << 9))) point.y = floor.point.y;
                            spots.Add(new Spot { position = point, label = "Path Soul" });
                        }
                }
            }
        }

        // Out in the void around the paths, away from the tree, at the height of the nearest
        // path sample.
        void AddVoidSpots(List<Spot> spots, float density)
        {
            if (roads.Count == 0) return;
            var bounds = new Bounds(roads[0].points[0], Vector3.zero);
            foreach (var road in roads)
                foreach (var point in road.points) bounds.Encapsulate(point);
            bounds.Expand(new Vector3(24, 0, 24));
            var tree = GameRoot.Instance != null ? GameRoot.Instance.Tree : null;
            int wanted = Mathf.RoundToInt(freeSpiritCount * density), added = 0;
            for (int i = 0; i < wanted * 8 && added < wanted; i++)
            {
                var point = new Vector3(Mathf.Lerp(bounds.min.x, bounds.max.x, Mathf.Repeat(i * .618034f + .3f, 1)),
                    0, Mathf.Lerp(bounds.min.z, bounds.max.z, Mathf.Repeat(i * .414214f + .7f, 1)));
                if (InsideRoad(point, 3)) continue;
                if (tree != null && Vector2.Distance(new Vector2(point.x, point.z), new Vector2(tree.position.x, tree.position.z)) < 23) continue;
                float nearest = float.PositiveInfinity;
                foreach (var road in roads)
                    foreach (var sample in road.points)
                    {
                        float d = new Vector2(point.x - sample.x, point.z - sample.z).sqrMagnitude;
                        if (d < nearest)
                        { nearest = d; point.y = sample.y + Mathf.Sin(i * 2.4f) * 2; }
                    }
                spots.Add(new Spot { position = point, label = "Void Soul" });
                added++;
            }
        }

        // Groups of three small yellow crystals along the path edges, alternating sides,
        // skipping spots a hand-placed crystal already holds.
        void LayCrystals()
        {
            foreach (var road in roads)
            {
                float distance = 0, next = 12;
                var collider = road.path.GetComponent<MeshCollider>();
                for (int i = 1; i < road.points.Count - 1; i++)
                {
                    distance += Vector3.Distance(road.points[i - 1], road.points[i]);
                    if (distance < next) continue;
                    next += Mathf.Max(5, crystalSpacing);
                    Vector3 forward = (road.points[i + 1] - road.points[i - 1]).normalized;
                    Vector3 side = Vector3.Cross(Vector3.up, forward).normalized;
                    int sign = (int)(distance / Mathf.Max(5, crystalSpacing)) % 2 == 0 ? 1 : -1;
                    for (int c = 0; c < 3 && CrystalCount < maxCrystals; c++)
                    {
                        var point = road.points[i] + side * sign * Mathf.Max(.5f, road.halfWidth - .65f) + forward * ((c - 1) * 1.05f);
                        if (!collider.Raycast(new Ray(point + Vector3.up * 2, Vector3.down), out var hit, 4)) continue;
                        bool occupied = false;
                        foreach (var other in Physics.OverlapSphere(hit.point + Vector3.up * .45f, .55f))
                            if (other.GetComponentInParent<BreakableCrystal>() != null)
                            { occupied = true; break; }
                        if (occupied) continue;
                        var crystal = Instantiate(crystalPrefab, hit.point, Quaternion.identity, crystalRoot);
                        crystal.name = "Group Crystal " + (++CrystalCount);
                        crystal.color = CrystalColor.Yellow;
                        crystal.size = CrystalSize.Small;
                        crystal.startsHidden = false;
                        crystal.ResetCrystal();
                        Physics.SyncTransforms();
                    }
                }
            }
        }

        // A prison phase or a broken crystal frees souls: they wander where it happened until
        // the torch calls them. Not path souls, so expedition starts leave them alone.
        void OnSoulsReleased(Vector3 position, int count)
        {
            if (spiritPrefab == null || torch == null || soulRoot == null) return;
            for (int i = 0; i < count; i++)
            {
                float angle = (spawned + i) * 2.4f;
                Vector3 point = position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * Balance.ReleasedSoulScatterRadius * Mathf.Sqrt((i + 1f) / count);
                if (Physics.Raycast(point + Vector3.up * 3, Vector3.down, out var floor, 8, (1 << 8) | (1 << 9), QueryTriggerInteraction.Ignore)) point.y = floor.point.y;
                Spawn(point, "Released Soul");
            }
        }

        WanderingSpirit Spawn(Vector3 point, string label)
        {
            var soul = Instantiate(spiritPrefab, point, Quaternion.identity, soulRoot);
            spawned++;
            soul.name = label + " " + spawned;
            soul.torch = torch;
            soul.phase = spawned * 2.4f;
            soul.allowFloatingOutsideGround = true;
            return soul;
        }
    }
}
