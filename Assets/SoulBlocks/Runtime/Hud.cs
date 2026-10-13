using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SoulBlocks
{
    // Shared IMGUI surface: every panel registers a draw callback instead of adding its own
    // OnGUI, so draw order stays controllable and there is exactly one GUI.skin to agree on.
    // Persistent text is small and translucent, stacked in two side columns: player stats on the
    // left (Line), plan/objective/blessings/toasts on the right (SideLine). Modals (shop, pause,
    // blessing pick, start menu, victory) still use plain GUI.Window, on a solid background.
    public sealed class Hud : MonoBehaviour
    {
        struct Entry { public int order; public Action draw; }
        struct ToastEntry { public string text; public float expires; }

        static readonly List<Entry> entries = new List<Entry>();
        static readonly List<ToastEntry> toasts = new List<ToastEntry>();
        const float LineHeight = 16f, ColumnWidth = 360f, Margin = 8f, ButtonWidth = 56f;
        static float leftY, rightY;
        static GUIStyle subtle, shadow, smallButton;
        static GUISkin skin;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void RegisterSystem() => GameRoot.Register<Hud>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { entries.Clear(); toasts.Clear(); }

        public static void Register(int order, Action draw) => entries.Add(new Entry { order = order, draw = draw });

        public static void Unregister(Action draw) => entries.RemoveAll(e => e.draw == draw);

        // Unscaled time: the pause menu sets timeScale 0 and its "Partida guardada" must still fade.
        public static void Toast(string text, float seconds = 2f) => toasts.Add(new ToastEntry { text = text, expires = Time.unscaledTime + seconds });

        // Left column row. Call from inside a registered draw callback; rows stack in Hud order
        // (100 expedition, 200 prisons, 300 abilities...).
        public static void Line(string text)
        {
            SubtleLabel(new Rect(Margin, leftY, ColumnWidth, LineHeight), text);
            leftY += LineHeight;
        }

        // Right column row, right-aligned, with an optional small button at its end. Returns true
        // the frame the button is clicked.
        public static bool SideLine(string text, string button = null)
        {
            float right = Screen.width - Margin;
            bool clicked = false;
            float textRight = right;
            if (button != null)
            {
                clicked = GUI.Button(new Rect(right - ButtonWidth, rightY, ButtonWidth, LineHeight), button, smallButton);
                textRight -= ButtonWidth + 6f;
            }
            SubtleLabel(new Rect(textRight - ColumnWidth, rightY, ColumnWidth, LineHeight), text, TextAnchor.UpperRight);
            rightY += button != null ? LineHeight + 2f : LineHeight;
            return clicked;
        }

        // Small translucent text with a 1 px shadow so it still reads over the bright tree.
        // GUI.color still tints it (the energy warning uses that).
        public static void SubtleLabel(Rect rect, string text, TextAnchor anchor = TextAnchor.UpperLeft)
        {
            subtle.alignment = shadow.alignment = anchor;
            GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text, shadow);
            GUI.Label(rect, text, subtle);
        }

        // Call from inside a registered draw callback, after GUI layout for the frame started.
        public static void WorldLabel(Vector3 world, string text)
        {
            var camera = GameRoot.Instance != null ? GameRoot.Instance.ViewCamera : null;
            if (camera == null) return;
            Vector3 screen = camera.WorldToScreenPoint(world);
            if (screen.z <= 0f) return;
            float y = Screen.height - screen.y;
            Vector2 size = subtle.CalcSize(new GUIContent(text));
            SubtleLabel(new Rect(screen.x - size.x * .5f, y - size.y * .5f, size.x, size.y), text, TextAnchor.MiddleCenter);
        }

        static void EnsureStyles()
        {
            if (skin != null) return;
            // A copy, so the builtin skin (which outlives play mode in the editor) stays untouched.
            skin = Instantiate(GUI.skin);
            var solid = new Texture2D(1, 1);
            solid.SetPixel(0, 0, new Color32(28, 26, 38, 255));
            solid.Apply();
            skin.window.normal.background = skin.window.onNormal.background = solid;
            GUI.skin = skin;
            subtle = new GUIStyle(GUI.skin.label) { fontSize = 12, wordWrap = false, clipping = TextClipping.Overflow };
            subtle.normal.textColor = new Color(1f, 1f, 1f, .75f);
            shadow = new GUIStyle(subtle);
            shadow.normal.textColor = new Color(0f, 0f, 0f, .6f);
            smallButton = new GUIStyle(GUI.skin.button) { fontSize = 11, padding = new RectOffset(2, 2, 1, 1) };
        }

        void OnGUI()
        {
            EnsureStyles();
            GUI.skin = skin;
            leftY = rightY = Margin;
            foreach (var entry in entries.OrderBy(e => e.order).ToArray())
                entry.draw?.Invoke();

            toasts.RemoveAll(t => t.expires <= Time.unscaledTime);
            if (toasts.Count > 0) rightY += 6f;
            foreach (var toast in toasts) SideLine(toast.text);
        }
    }
}
