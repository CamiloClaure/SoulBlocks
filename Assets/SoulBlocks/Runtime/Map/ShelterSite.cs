using UnityEngine;

namespace SoulBlocks
{
    // A shelter placed by hand: energy stops draining inside its radius (ShelterZone) and, with
    // Entrega, the souls following the torch can be delivered here. SceneLayout gives it its
    // id; on MapBuilt it lays its pale disc and the zone.
    public sealed class ShelterSite : MonoBehaviour
    {
        [Min(1f)] public float radius = 6f;
        public int Id { get; set; } = -1;
        GameObject disc;

        void OnEnable() => GameEvents.MapBuilt += OnMapBuilt;
        void OnDisable() => GameEvents.MapBuilt -= OnMapBuilt;

        void OnMapBuilt(MapLayout layout)
        {
            if (disc != null) Destroy(disc);
            // ponytail: Placeholder's shared material is opaque, so a pale emissive cyan stands
            // in for a translucent disc.
            float diameter = radius * 2f;
            disc = Placeholder.Make(PrimitiveType.Cylinder, new Color(.45f, .8f, .9f), "Shelter " + Id,
                transform, transform.position + Vector3.up * .03f, new Vector3(diameter, .02f, diameter), false, .15f);
            var zone = disc.AddComponent<ShelterZone>();
            zone.ShelterId = Id;
            zone.Radius = radius;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(.45f, .8f, .9f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
