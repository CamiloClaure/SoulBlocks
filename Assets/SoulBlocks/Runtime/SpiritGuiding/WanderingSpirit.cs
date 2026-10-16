using UnityEngine;

namespace SoulBlocks
{
    public sealed class WanderingSpirit : MonoBehaviour
    {
        public GuidingTorch torch;
        public Transform visual;
        public float wanderRadius = 2f;
        public float wanderSpeed = 4f;
        public float followSpeed = 9f;
        public float phase;
        public bool allowFloatingOutsideGround;
        PathPopulation population;
        public bool Following { get; private set; }
        public bool Ascending { get; private set; }
        public bool FollowingBehind => Following && torch != null && torch.State == TorchState.Carried;
        Vector3 home;
        float ascension;
        bool delivered;
        Vector3[] road;
        float roadHalfWidth;
        void Start()
        {
            home = transform.position;
            population = FindAnyObjectByType<PathPopulation>();
            if (population != null) return;
            var go = GameObject.Find("Camino primario");
            var path = go != null ? go.GetComponent<AuthoredPath>() : null;
            if (path == null) return;
            roadHalfWidth = path.width * Mathf.Max(Mathf.Abs(path.transform.lossyScale.x), Mathf.Abs(path.transform.lossyScale.z)) * .5f + .5f;
            road = new Vector3[501];
            for (int i = 0; i < road.Length; i++) road[i] = path.transform.TransformPoint(path.Evaluate(i / 500f));
        }
        bool InsideRoad(Vector3 position)
        {
            if (population != null) return population.InsideRoad(position);
            if (road == null) return false;
            for (int i = 0; i < road.Length; i++)
            {
                var delta = road[i] - position; delta.y = 0;
                if (delta.sqrMagnitude < roadHalfWidth * roadHalfWidth) return true;
            }
            return false;
        }
        void Update()
        {
            if (Keys.Blocked || Time.deltaTime <= 0 || torch == null) return;
            if (Ascending)
            {
                ascension += Time.deltaTime;
                transform.position += Vector3.up * Time.deltaTime * 2;
                if (visual != null) visual.localScale = Vector3.one * Mathf.Max(0, 1 - ascension / 1.4f);
                if (ascension >= 1.4f && !delivered) { delivered = true; torch.ReceiveSoul(); Destroy(gameObject); }
                return;
            }
            bool attracted = torch.Calls(transform.position, Following);
            if (Following && !attracted) home = transform.position;
            Following = attracted;
            var tree = Following ? null : NourishableTree.Nearest(transform.position);
            Vector3 target;
            if (Following)
            {
                target = torch.transform.position;
                // Give each spirit its own spot around the light rather than stacking them.
                target += new Vector3(Mathf.Cos(phase), 0, Mathf.Sin(phase)) * .8f;
                if (torch.State == TorchState.Carried && GameRoot.Instance != null && GameRoot.Instance.Player != null)
                {
                    var player = GameRoot.Instance.Player;
                    // A stable spread behind the player, with no circling or orbiting.
                    var offset = new Vector3(Mathf.Sin(phase) * .9f,
                        .55f + Mathf.Sin(phase * 1.7f) * .2f,
                        -1.8f - Mathf.Repeat(phase * .618f, 1) * 2.2f);
                    target = player.transform.position + Quaternion.Euler(0, player.Yaw, 0) * offset;
                }
                if (torch.State == TorchState.Altar && Vector3.Distance(transform.position, torch.AltarPosition) < 2.4f)
                { Ascending = true; return; }
            }
            else if (tree != null)
            {
                home = transform.position;
                target = tree.transform.position + new Vector3(Mathf.Cos(phase + Time.time * .3f), 0, Mathf.Sin(phase + Time.time * .3f)) * 1.5f;
            }
            else target = home + new Vector3(Mathf.Sin(Time.time * 2.4f + phase), 0, Mathf.Cos(Time.time * 1.9f + phase)) * wanderRadius;
            if (Following)
            {
                // Guided spirits fly freely in three dimensions instead of being flattened onto the floor.
                transform.position = Vector3.MoveTowards(transform.position, target, followSpeed * Time.deltaTime);
                if (visual != null) visual.localPosition = Vector3.up * .55f;
                return;
            }
            Vector3 delta = target - transform.position; delta.y = 0;
            Vector3 next = transform.position + Vector3.ClampMagnitude(delta, (Following ? followSpeed : wanderSpeed) * Time.deltaTime);
            if (!Following && tree == null && InsideRoad(next)) next = transform.position;
            // Spirits travel over authored ground, so attraction cannot send them through the void.
            if (Physics.Raycast(next + Vector3.up * 3, Vector3.down, out var floor, 8, (1 << 8) | (1 << 9), QueryTriggerInteraction.Ignore))
            {
                next.y = floor.point.y;
                transform.position = next;
                if (delta.sqrMagnitude > .01f) transform.rotation = Quaternion.LookRotation(delta.normalized);
            }
            else if (allowFloatingOutsideGround)
            {
                next.y = Following ? Mathf.MoveTowards(transform.position.y, target.y - .5f, followSpeed * Time.deltaTime) : home.y;
                transform.position = next;
            }
            if (visual != null) visual.localPosition = Vector3.up * (.55f + Mathf.Sin(Time.time * 3 + phase) * .08f);
        }
    }
}
