using UnityEngine;

namespace SoulBlocks
{
    // A fixed crystal spot in the lab or the arena: Restore() respawns its crystal fresh.
    public sealed class CrystalLabSlot : MonoBehaviour
    {
        public GameObject prefab;
        public BreakableCrystal crystal;
        public CrystalColor color;
        public CrystalSize size;
        public bool hidden;

        public void Restore()
        {
            if (crystal != null)
            { crystal.gameObject.SetActive(false); Destroy(crystal.gameObject); }
            var go = Instantiate(prefab, transform);
            go.name = "Crystal " + color + " " + size;
            go.transform.localPosition = Vector3.zero;
            crystal = go.GetComponent<BreakableCrystal>();
            crystal.color = color;
            crystal.size = size;
            crystal.startsHidden = hidden;
            crystal.rewardSouls = 3 * (int)size;
            crystal.ResetCrystal();
        }
    }
}
