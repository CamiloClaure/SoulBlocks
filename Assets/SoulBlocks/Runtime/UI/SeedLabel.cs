using UnityEngine;

namespace SoulBlocks
{
    // Player-facing seed readout, bottom-right, always visible (no Keys check: it draws under
    // menus too since it registers at the very top of the 800-899 range).
    public sealed class SeedLabel : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() => GameRoot.Register<SeedLabel>();

        void OnEnable() => Hud.Register(899, Draw);
        void OnDisable() => Hud.Unregister(Draw);

        static void Draw()
        {
            var run = GameRoot.Instance != null ? GameRoot.Instance.Run : null;
            string text = "Seed " + (run != null ? run.effectiveSeed.ToString() : "-");
            Hud.SubtleLabel(new Rect(Screen.width - 208f, Screen.height - 22f, 200f, 16f), text, TextAnchor.LowerRight);
        }
    }
}
