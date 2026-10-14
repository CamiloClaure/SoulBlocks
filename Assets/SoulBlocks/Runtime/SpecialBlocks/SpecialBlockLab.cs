using UnityEngine;

namespace SoulBlocks
{
    // Authored scene objects; only the reset button respawns their crystals.
    public sealed class SpecialBlockLab : MonoBehaviour
    {
        public Transform tree;
        public CrystalLabSlot[] slots;
        public LabSurface[] surfaces;
        public Renderer[] blockDisplays;
        public void RestoreCrystals()
        {
            foreach (var slot in slots) if (slot != null) slot.Restore();
            foreach (var surface in surfaces) if (surface != null) surface.ResetLight();
        }
    }
}
