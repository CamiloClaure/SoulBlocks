using System.Collections.Generic;
using UnityEngine;

namespace SoulBlocks
{
    public sealed class NourishableTree : MonoBehaviour
    {
        [Tooltip("Reemplaza el contenido de Model por tu FBX y sus animaciones.")]
        public Transform model;
        public WanderingSpirit spiritPrefab;
        [Min(1)] public float spiritInterval = 5;
        [Min(1)] public int maxSpirits = 3;
        [Min(1)] public float attractionRadius = 7;
        public bool Alive { get; private set; }
        public static readonly List<NourishableTree> All = new List<NourishableTree>();
        readonly List<WanderingSpirit> spawned = new List<WanderingSpirit>();
        float nextSpirit;
        bool wasAlive;
        Light glow;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ClearRegistry() => All.Clear();
        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable() => All.Remove(this);
        void Start()
        {
            var lightObject = new GameObject("Luz del árbol vivo");
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.localPosition = Vector3.up * 1.5f;
            glow = lightObject.AddComponent<Light>();
            glow.color = Color.yellow; glow.range = attractionRadius; glow.intensity = 3;
            Refresh();
        }
        public void Revive(float seconds)
        {
            // Existing callers provide a light duration; the revived tree now stays alive until reset.
            bool waking = !Alive;
            if (waking) nextSpirit = Time.time + spiritInterval;
            Alive = true;
            if (waking) Refresh();
        }
        public void ResetTree()
        {
            Alive = false;
            foreach (var spirit in spawned) if (spirit != null) Destroy(spirit.gameObject);
            spawned.Clear(); Refresh();
        }
        void Update()
        {
            if (Keys.Blocked || Time.deltaTime <= 0) return;
            if (Alive != wasAlive) Refresh();
            if (!Alive || Time.time < nextSpirit || spiritPrefab == null) return;
            nextSpirit = Time.time + Mathf.Max(1, spiritInterval);
            spawned.RemoveAll(s => s == null);
            if (spawned.Count >= maxSpirits) return;
            var torch = FindAnyObjectByType<GuidingTorch>();
            if (torch == null) return;
            float angle = spawned.Count * 2.4f + Time.time;
            Vector3 position = transform.position + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 1.5f;
            var spirit = Instantiate(spiritPrefab, position, Quaternion.identity);
            spirit.name = "Tree Soul";
            spirit.torch = torch; spirit.allowFloatingOutsideGround = true; spirit.phase = angle;
            spawned.Add(spirit);
        }
        void Refresh()
        {
            wasAlive = Alive;
            if (glow != null) glow.enabled = Alive;
            var tint = new MaterialPropertyBlock();
            tint.SetColor("_BaseColor", Alive ? Color.yellow : Color.gray);
            tint.SetColor("_Color", Alive ? Color.yellow : Color.gray);
            if (model == null) return;
            foreach (var renderer in model.GetComponentsInChildren<Renderer>()) renderer.SetPropertyBlock(tint);
            var animator = model.GetComponentInChildren<Animator>();
            if (animator != null && animator.runtimeAnimatorController != null)
                foreach (var parameter in animator.parameters)
                    if (parameter.name == "Alive" && parameter.type == AnimatorControllerParameterType.Bool)
                        animator.SetBool("Alive", Alive);
        }
        public static NourishableTree Nearest(Vector3 position)
        {
            NourishableTree nearest = null;
            float distance = float.PositiveInfinity;
            foreach (var tree in All)
            {
                if (tree == null || !tree.Alive) continue;
                float d = Vector3.Distance(position, tree.transform.position);
                if (d <= tree.attractionRadius && d < distance) { nearest = tree; distance = d; }
            }
            return nearest;
        }
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, attractionRadius);
        }
    }
}
