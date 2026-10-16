using UnityEngine;

namespace SoulBlocks
{
    // The power test arena's crystal slots, restored together with the lab.
    public sealed class PowerupTestArena : MonoBehaviour
    {
        public CrystalLabSlot[] slots;
        public void Restore()
        {
            if (slots == null) return;
            foreach (var slot in slots)
                if (slot != null)
                    slot.Restore();
        }
    }
}
