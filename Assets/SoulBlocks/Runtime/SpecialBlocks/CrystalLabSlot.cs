using UnityEngine;

namespace SoulBlocks
{
    public sealed class CrystalLabSlot : MonoBehaviour
    {
        public GameObject prefab;
        public BreakableCrystal crystal;
        public CrystalColor color;
        public CrystalSize size;
        public bool hidden;

        public void Restore()
        {
            if (crystal != null) { crystal.gameObject.SetActive(false); Destroy(crystal.gameObject); }
            var go = Instantiate(prefab, transform);
            go.name = color == CrystalColor.Yellow && size == CrystalSize.Small ? "Crystal Yellow - Tree" : color + " " + size;
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
