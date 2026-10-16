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
        void Start()
        {
            // Blue now nourishes rewards; the lab no longer depends on a reveal power.
            foreach (var slot in slots)
            {
                if (slot == null) continue;
                slot.hidden = false;
                if (slot.crystal == null) continue;
                slot.crystal.startsHidden = false;
                slot.crystal.Reveal();
            }
        }

        public void RestoreCrystals()
        {
            foreach (var slot in slots)
                if (slot != null)
                    slot.Restore();
            foreach (var surface in surfaces)
                if (surface != null)
                    surface.ResetLight();
            foreach (var arena in FindObjectsByType<PowerupTestArena>()) arena.Restore();
        }
    }
}
