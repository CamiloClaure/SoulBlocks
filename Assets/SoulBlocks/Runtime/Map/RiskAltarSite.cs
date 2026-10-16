using UnityEngine;

namespace SoulBlocks
{
    // A risk altar placed by hand (AltarSystem builds the pillar and rolls its offer).
    public sealed class RiskAltarSite : MonoBehaviour
    {
        void OnDrawGizmos()
        {
            Gizmos.color = new Color(.55f, .1f, .75f);
            Gizmos.DrawWireCube(transform.position + Vector3.up * .6f, new Vector3(.6f, 1.2f, .6f));
        }
    }
}
