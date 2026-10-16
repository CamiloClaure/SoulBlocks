using UnityEngine;

namespace SoulBlocks
{
    // A crystal shard dropped by a broken crystal: scatters, settles and is picked up by walking
    // over it (or by a celeste helper), adding one to Run.collectedCrystals.
    public sealed class CrystalFragment : MonoBehaviour
    {
        [Tooltip("Reemplaza el contenido de Model por tu FBX. Puedes añadir un Animator dentro de Model.")]
        public Transform model;
        [Min(.1f)] public float pickupRadius = 1.1f;
        [Min(0)] public float pickupDelay = .7f;
        public CrystalColor Color { get; private set; }
        public bool Collected { get; private set; }
        public const int Value = 1;
        Vector3 start, landing;
        float born;
        bool initialized;
        const float ScatterSeconds = .45f;

        public void Initialize(Vector3 from, Vector3 to, CrystalColor color, Color tint)
        {
            start = from;
            landing = to;
            Color = color;
            born = Time.time;
            initialized = true;
            transform.position = from;
            var properties = new MaterialPropertyBlock();
            properties.SetColor("_BaseColor", tint);
            properties.SetColor("_Color", tint);
            foreach (var renderer in (model != null ? model : transform).GetComponentsInChildren<Renderer>())
                renderer.SetPropertyBlock(properties);
        }

        void Start()
        {
            // A fragment dragged directly into a scene is also collectible.
            if (!initialized)
            { start = landing = transform.position; born = Time.time; initialized = true; }
        }

        void Update()
        {
            if (!initialized || Collected || Keys.Blocked || Time.deltaTime <= 0) return;
            float t = Mathf.Clamp01((Time.time - born) / ScatterSeconds);
            transform.position = Vector3.Lerp(start, landing, t) + Vector3.up * (4 * t * (1 - t) * .55f);
            var root = GameRoot.Instance;
            if (root != null) TryCollect(root.Player);
        }

        public bool TryCollect(SoulPlayer player)
            => player != null && TryCollectAt(player.transform.position);
        public bool TryCollectAt(Vector3 collectorPosition)
        {
            var root = GameRoot.Instance;
            if (Collected || !initialized || Time.time - born < Mathf.Max(pickupDelay, ScatterSeconds)
                || Keys.Blocked || Time.timeScale <= 0 || root == null || root.Run == null)
                return false;
            Vector3 delta = collectorPosition - transform.position;
            if (Mathf.Abs(delta.y) > 2 || new Vector2(delta.x, delta.z).sqrMagnitude > pickupRadius * pickupRadius) return false;
            Collected = true;
            root.Run.collectedCrystals += Value;
            gameObject.SetActive(false);
            Destroy(gameObject);
            return true;
        }

        public static void Scatter(BreakableCrystal crystal)
        {
            var prefab = crystal.fragmentPrefab != null ? crystal.fragmentPrefab : Resources.Load<CrystalFragment>("Crystals/CrystalFragment");
            if (prefab == null)
            { Debug.LogError("Falta el prefab Resources/Crystals/CrystalFragment.", crystal); return; }
            int count = crystal.FragmentReward;
            Vector3 origin = crystal.transform.position;
            var root = GameRoot.Instance;
            Transform parent = root != null ? root.transform : null;
            for (int i = 0; i < count; i++)
            {
                float angle = i * Mathf.PI * 2 / count + .35f;
                var candidate = origin + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * (.55f + .15f * (i % 3));
                Vector3 target = origin + Vector3.up * .15f;
                if (Physics.Raycast(candidate + Vector3.up, Vector3.down, out var hit, 3f, (1 << 8) | (1 << 9), QueryTriggerInteraction.Ignore))
                    target = hit.point + Vector3.up * .15f;
                var fragment = Instantiate(prefab, origin, Quaternion.identity, parent);
                fragment.name = "Fragmento " + crystal.color + " " + (i + 1);
                fragment.Initialize(origin + Vector3.up * .4f, target, crystal.color, crystal.BaseColor);
            }
        }
    }
}
