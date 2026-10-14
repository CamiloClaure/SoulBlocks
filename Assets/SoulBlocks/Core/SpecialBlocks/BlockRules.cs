using UnityEngine;

namespace SoulBlocks
{
    public enum SpecialBlock { Yellow, Blue, Green, Celeste, Red }

    public static class BlockRules
    {
        public const int MaxLevel = 5;
        public const int MaxHelpers = 4;
        public const float StepDistance = 1.5f;
        public const float BombFuse = 2.5f;
        public static int HitInterval(int level) => Mathf.Max(2, 10 - 2 * (Mathf.Clamp(level, 1, MaxLevel) - 1));
        public static float Chance(int clothingLevel) => .2f + .06f * Mathf.Clamp(clothingLevel, 0, MaxLevel);
        public static int RedDamage(int level, bool helper = false) => Mathf.Max(1, (4 + 2 * (level - 1)) / (helper ? 2 : 1));
        public static float Radius(SpecialBlock block, int level) => block == SpecialBlock.Red ? 2f + .6f * (level - 1) : 4f + level;
        public static float Duration(SpecialBlock block, int level) => block == SpecialBlock.Celeste ? 12f + 4f * (level - 1) : 8f + 2f * (level - 1);
        public static float Speed(int level) => 1.35f + .15f * (level - 1);
        public static float BuffSeconds(int level) => 1f + .5f * (level - 1);
        public static int HelperCount(int level) => 1 + (level - 1) / 2;
        public static string Name(SpecialBlock block) => new[] { "Amarillo", "Azul", "Verde", "Celeste", "Rojo" }[(int)block];
        public static Color ColorOf(SpecialBlock block) => new[] { new Color(1, .8f, .05f), new Color(.12f, .25f, 1), new Color(.15f, 1, .3f), new Color(.1f, .9f, 1), new Color(1, .15f, .1f) }[(int)block];
    }
}
