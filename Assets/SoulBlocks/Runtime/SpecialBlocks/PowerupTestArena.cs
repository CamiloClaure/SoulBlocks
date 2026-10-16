using UnityEngine;

namespace SoulBlocks
{
    public sealed class PowerupTestArena : MonoBehaviour
    {
        public CrystalLabSlot[] slots;
        public void Restore()
        {
            if (slots == null) return;
            foreach (var slot in slots) if (slot != null) slot.Restore();
        }
    }
}
