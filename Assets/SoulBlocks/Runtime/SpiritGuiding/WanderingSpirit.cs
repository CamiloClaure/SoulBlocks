using System.Collections.Generic;
using UnityEngine;

namespace SoulBlocks
{
    // A soul loose in the world. It wanders off the paths (or circles a nourishable tree) until
    // a lit torch calls it, then follows the light: behind the player while carried, around it
    // when placed. At the altar (torch returned) or a shelter with Entrega it ascends and pays
    // one soul into the reserve. At most StatId.SpiritCapacity souls follow at once (Carga).
    public sealed class WanderingSpirit : MonoBehaviour
    {
        public static readonly List<WanderingSpirit> All = new List<WanderingSpirit>();
        public static int FollowingCount { get; private set; }

        public GuidingTorch torch;
        public Transform visual;
        public float wanderRadius = 2f;
        public float wanderSpeed = 4f;
        public float followSpeed = 9f;
        public float phase;
        public bool allowFloatingOutsideGround;
        PathPopulation population;
        bool following;
        public bool Following
        {
            get => following;
            private set
            {
                if (following == value) return;
                following = value;
                FollowingCount += value ? 1 : -1;
            }
        }
        public bool Ascending { get; private set; }
        public bool FollowingBehind => Following && torch != null && torch.State == TorchState.Carried;
        Vector3 home;
        float ascension;
        bool delivered;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            All.Clear();
            FollowingCount = 0;
        }

        static int Capacity => GameRoot.Instance != null && GameRoot.Instance.Stats != null ? GameRoot.Instance.Stats.GetInt(StatId.SpiritCapacity) : int.MaxValue;

        void OnEnable() => All.Add(this);

        void OnDisable()
        {
            All.Remove(this);
            Following = false;
        }

        // Where it was spawned (or last let go): it wanders around this point.
        public Vector3 Home => home;

        void Awake() => home = transform.position;

        void Start() => population = FindAnyObjectByType<PathPopulation>();

        // Shelters with Entrega deliver the souls following the torch on the spot.
        public void Deliver() => Ascending = true;

        bool InsideRoad(Vector3 position) => population != null && population.InsideRoad(position);

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
            if (attracted && !Following && FollowingCount >= Capacity) attracted = false;
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
