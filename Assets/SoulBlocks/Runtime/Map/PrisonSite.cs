using UnityEngine;

namespace SoulBlocks
{
    // A crystal prison placed by hand. SceneLayout turns it into a PrisonLayout and
    // PrisonSystem spawns the PrisonView on this spot at runtime; the site itself only holds
    // the data. Prisons sharing a groupId (>= 0) resonate together.
    public sealed class PrisonSite : MonoBehaviour
    {
        public PrisonTier tier;
        public PrisonProp props;
        [Tooltip("Prisiones con el mismo grupo (>= 0) resuenan entre sí.")]
        public int groupId = -1;
        public PrisonPattern pattern;
        [Tooltip("Prisión al final de su rama: al romperla ofrece una bendición y abre el portal de la rama.")]
        public bool branchEnd;

        void OnDrawGizmos()
        {
            Gizmos.color = tier == PrisonTier.Nucleo ? Color.white : branchEnd ? Color.magenta : Color.yellow;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * .6f, tier == PrisonTier.Nucleo ? 1.4f : .8f);
        }
    }
}
