using System;
using UnityEngine;

namespace SoulBlocks
{
    // Placeholder pickup: a small dark violet sphere with emission so it reads on the dark
    // tiles. Registers itself in SoulRegistry.All so SoulCollector and the magnet (habilidades)
    // can find it generically.
    public sealed class SoulPickup : MonoBehaviour, ISoul
    {
        const float Diameter = .35f;
        static readonly Color DarkViolet = new Color(.35f, .05f, .55f);

        public int Value { get; private set; } = 1;
        public bool Available { get; private set; } = true;
        public Vector3 Position => transform.position;

        // Raised right before the GameObject is destroyed; SoulSpawner uses it to drop the
        // matching Run.groundSouls record for loose souls.
        public event Action OnCollected;

        void OnEnable() => SoulRegistry.All.Add(this);
        void OnDisable() => SoulRegistry.All.Remove(this);

        public void MoveTo(Vector3 position) => transform.position = position;

        public void Collect()
        {
            if (!Available) return;
            Available = false;
            OnCollected?.Invoke();
            Destroy(gameObject);
        }

        public static SoulPickup Spawn(Transform parent, Vector3 position, int value)
        {
            var go = Placeholder.Make(PrimitiveType.Sphere, DarkViolet, "Soul", parent, position,
                Vector3.one * Diameter, false, 1.4f);
            var pickup = go.AddComponent<SoulPickup>();
            pickup.SetValue(value);
            return pickup;
        }

        void SetValue(int value) => Value = value;
    }
}
