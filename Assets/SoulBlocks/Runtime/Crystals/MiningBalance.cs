using UnityEngine;
namespace SoulBlocks
{
    [CreateAssetMenu(menuName = "SoulBlocks/Balance de minería")]
    public sealed class MiningBalance : ScriptableObject
    {
        public int[] damagePerLevel = { 1, 2, 3, 5, 7 };
        public int[] crystalHealth = { 10, 150, 1000, 1500, 3000 };
        [Min(1)] public int violetHealth = 8;
        [Tooltip("Segundos por golpe al comenzar cada etapa.")]
        public float[] startingSeconds = { .55f, .49f, .43f, .37f, .31f };
        [Min(0)] public float secondsFasterPerLevel = .012f;
        [Min(.1f)] public float minimumSeconds = .15f;
        [Min(0)] public int firstUpgradeCost = 10;
        [Min(0)] public int additionalCostPerLevel = 5;
        static MiningBalance cached;
        public static MiningBalance Current => cached != null ? cached : cached = Resources.Load<MiningBalance>("Mining/Balance") ?? CreateInstance<MiningBalance>();
        public static int Stage(int level) => (Mathf.Clamp(level, 1, 50) - 1) / 10;
        public int Damage(int level)
        {
            int result = 0;
            for (int i = 1; i <= Mathf.Clamp(level, 1, 50); i++) result += Mathf.Max(1, damagePerLevel[Stage(i)]);
            return result;
        }
        public float Seconds(int level) => Mathf.Max(minimumSeconds, startingSeconds[Stage(level)] - ((Mathf.Clamp(level, 1, 50) - 1) % 10) * secondsFasterPerLevel);
        public int Cost(int level) => Mathf.Max(0, firstUpgradeCost) + (Mathf.Clamp(level, 1, 50) - 1) * Mathf.Max(0, additionalCostPerLevel);
        public int Health(CrystalColor color)
        {
            int index = color == CrystalColor.Blue ? 1 : color == CrystalColor.Green ? 2 : color == CrystalColor.Celeste ? 3 : color == CrystalColor.Red ? 4 : 0;
            return Mathf.Max(1, color == CrystalColor.Violet ? violetHealth : crystalHealth[index]);
        }
    }
}
