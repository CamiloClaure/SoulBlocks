using UnityEngine;

namespace SoulBlocks
{
    // Where a branch's portal ring appears once Raiz1 is owned and the branch-end prison is
    // broken (PortalSystem). Its branch is the AuthoredPath it sits under, or the nearest one.
    public sealed class PortalSite : MonoBehaviour
    {
        void OnDrawGizmos()
        {
            Gizmos.color = new Color(.5f, .3f, 1f);
            Gizmos.DrawWireSphere(transform.position, Balance.PortalDiameter * .5f);
        }
    }
}
