using System;
using UnityEngine;

namespace SoulBlocks
{
    // Placeholder pickup: a small dark violet sphere with emission, floating FloatHeight above
    // the ground with a gentle bob, bursting into violet specks when collected. Position/MoveTo work on the steady (un-bobbed) point so the
    // magnet and the collector never fight the bob. Registers itself in SoulRegistry.All so
    // SoulCollector and the magnet (habilidades) can find it generically.
    public sealed class SoulPickup : MonoBehaviour, ISoul
    {
        const float Diameter = .35f;
        public const float FloatHeight = 1f;
        const float BobAmplitude = .12f, BobSpeed = 2f;
        static readonly Color DarkViolet = new Color(.35f, .05f, .55f);
        static readonly Color BurstViolet = new Color(.7f, .3f, 1f);

        public int Value { get; private set; } = 1;
        public bool Available { get; private set; } = true;
        public Vector3 Position => basePosition;
        Vector3 basePosition;
        float bobPhase;

        // Raised right before the GameObject is destroyed; SoulSpawner uses it to drop the
        // matching Run.groundSouls record for loose souls.
        public event Action OnCollected;

        void OnEnable() => SoulRegistry.All.Add(this);
        void OnDisable() => SoulRegistry.All.Remove(this);

        public void MoveTo(Vector3 position) => basePosition = position;

        void Update() => transform.position = basePosition + Vector3.up * (Mathf.Sin(Time.time * BobSpeed + bobPhase) * BobAmplitude);

        public void Collect()
        {
            if (!Available) return;
            Available = false;
            OnCollected?.Invoke();
            Burst.Spawn(transform.position, BurstViolet);
            Destroy(gameObject);
        }

        // `ground` is a point on the path; the soul floats FloatHeight above it. Saved ground
        // souls keep the ground point, so loading never stacks the offset twice.
        public static SoulPickup Spawn(Transform parent, Vector3 ground, int value)
        {
            Vector3 position = ground + Vector3.up * FloatHeight;
            var go = Placeholder.Make(PrimitiveType.Sphere, DarkViolet, "Soul", parent, position,
                Vector3.one * Diameter, false, 1.4f);
            var pickup = go.AddComponent<SoulPickup>();
            pickup.SetValue(value);
            pickup.basePosition = position;
            pickup.bobPhase = (ground.x * .37f + ground.z * .61f) % (Mathf.PI * 2f);
            return pickup;
        }

        void SetValue(int value) => Value = value;
    }
}
