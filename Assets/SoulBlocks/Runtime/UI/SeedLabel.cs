using UnityEngine;

namespace SoulBlocks
{
    // Player-facing seed readout, bottom-right, always visible (no Keys check: it draws under
    // menus too since it registers at the very top of the 800-899 range). Hud.cs keeps its own
    // top-left debug line with the same number; this is the one players are meant to read.
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
            var size = GUI.skin.label.CalcSize(new GUIContent(text));
            GUI.Label(new Rect(Screen.width - size.x - 8f, Screen.height - size.y - 6f, size.x, size.y), text);
        }
    }
}
