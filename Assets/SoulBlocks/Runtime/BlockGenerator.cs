using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public sealed class BlockGenerator : MonoBehaviour
{
    public GameObject blockPrefab;
    public Transform spawnPoint;
    public Transform mainIsland;
    [Min(1)] public int blockCount = 30;
    public bool useIslandBounds = true;
    public float spreadX = 4f;
    public float spreadZ = 4f;
    public LayerMask groundMask = 1 << 8;
    public float rayHeight = 20f;
    public float rayDistance = 100f;
    public float surfaceOffset = .42f;
    [Min(1)] public int maxSpawnAttempts = 10000;
    [Min(.9f)] public float minimumSpacing = 2f;
    public Transform player;
    public bool spawnStarterBlocks = true;
    public int SpawnedCount { get; private set; }
    readonly List<Vector3> positions = new List<Vector3>();
    Bounds islandBounds;
    Vector3 halfSize;
    float castTop;
    float castLength;

    void Start() => Generate();

    public void Generate()
    {
        if (SpawnedCount > 0) return;
        if (blockPrefab == null || blockPrefab.GetComponent<PickableBlock>() == null ||
            blockPrefab.GetComponent<BoxCollider>() == null)
        { Debug.LogError("BlockGenerator needs a prefab with PickableBlock and BoxCollider.", this); return; }
        if (mainIsland == null) mainIsland = GameObject.Find("Main Island")?.transform;
        if (mainIsland == null) { Debug.LogError("Main Island is missing.", this); return; }
        if (groundMask.value == 0) groundMask = 1 << 8;

        bool found = false;
        foreach (Collider collider in mainIsland.GetComponentsInChildren<Collider>())
        {
            if (!collider.enabled || collider.isTrigger ||
                (groundMask.value & (1 << collider.gameObject.layer)) == 0) continue;
            if (!found) { islandBounds = collider.bounds; found = true; }
            else islandBounds.Encapsulate(collider.bounds);
        }
        if (!found) { Debug.LogError("Island needs ground colliders on Ground Mask.", this); return; }
        halfSize = Vector3.Scale(blockPrefab.GetComponent<BoxCollider>().size,
            blockPrefab.transform.localScale) * .5f;
        castTop = islandBounds.max.y + Mathf.Max(rayHeight, 2f);
        castLength = Mathf.Max(rayDistance, islandBounds.size.y + Mathf.Max(rayHeight, 2f) + 2f);
        Physics.SyncTransforms();

        if (spawnStarterBlocks && player != null)
        {
            Vector3[] offsets = { new Vector3(-2, 0, 0), new Vector3(2, 0, 0), new Vector3(0, 0, -2) };
            foreach (Vector3 offset in offsets)
                if (SpawnedCount < blockCount) TrySpawn(player.position + offset);
        }
        Vector3 center = spawnPoint != null ? spawnPoint.position : islandBounds.center;
        for (int attempt = 0; attempt < maxSpawnAttempts && SpawnedCount < blockCount; attempt++)
        {
            Vector3 sample = useIslandBounds
                ? new Vector3(Random.Range(islandBounds.min.x, islandBounds.max.x), 0,
                    Random.Range(islandBounds.min.z, islandBounds.max.z))
                : center + new Vector3(Random.Range(-spreadX, spreadX), 0, Random.Range(-spreadZ, spreadZ));
            TrySpawn(sample);
        }
        if (SpawnedCount < blockCount)
            Debug.LogWarning($"Only {SpawnedCount}/{blockCount} blocks fit on the island.", this);
    }

    bool TrySpawn(Vector3 sample)
    {
        Vector3 origin = new Vector3(sample.x, castTop, sample.z);
        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, castLength,
            groundMask, QueryTriggerInteraction.Ignore) || hit.normal.y < .95f) return false;
        for (int x = -1; x <= 1; x += 2)
        for (int z = -1; z <= 1; z += 2)
        {
            Vector3 corner = origin + new Vector3(x * (halfSize.x + .05f), 0, z * (halfSize.z + .05f));
            if (!Physics.Raycast(corner, Vector3.down, out RaycastHit support, castLength,
                groundMask, QueryTriggerInteraction.Ignore) || support.normal.y < .95f ||
                Mathf.Abs(support.point.y - hit.point.y) > .08f) return false;
        }
        Vector3 position = hit.point + Vector3.up * Mathf.Max(surfaceOffset, halfSize.y + .02f);
        foreach (Vector3 existing in positions)
            if ((existing - position).sqrMagnitude < minimumSpacing * minimumSpacing) return false;
        if (Physics.CheckBox(position, halfSize, Quaternion.identity,
            ~groundMask.value, QueryTriggerInteraction.Ignore)) return false;
        Instantiate(blockPrefab, position, Quaternion.identity);
        positions.Add(position);
        SpawnedCount++;
        Physics.SyncTransforms();
        return true;
    }
}

