using UnityEngine;

namespace SoulBlocks
{
    // Plain per-instance marker, not a GameRoot system: MapBuilder adds one of these to each
    // shelter placeholder it builds. Raises ShelterEntered/Exited only on the transition so
    // og/expedicion can gate the energy drain without polling distance itself.
    public sealed class ShelterZone : MonoBehaviour
    {
        public int ShelterId;
        public float Radius;
        bool inside;

        void Update()
        {
            var root = GameRoot.Instance;
            if (root == null || root.Player == null) return;
            Vector3 p = root.Player.transform.position;
            float dx = p.x - transform.position.x, dz = p.z - transform.position.z;
            bool now = dx * dx + dz * dz <= Radius * Radius;
            if (now == inside) return;
            inside = now;
            if (inside) GameEvents.RaiseShelterEntered(ShelterId);
            else GameEvents.RaiseShelterExited(ShelterId);
        }
    }
}
