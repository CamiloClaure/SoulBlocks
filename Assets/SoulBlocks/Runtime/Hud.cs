using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SoulBlocks
{
    // Shared IMGUI surface: every panel registers a draw callback instead of adding its own
    // OnGUI, so draw order stays controllable and there is exactly one GUI.skin to agree on.
    // The skin is flat and fully opaque (no default bevels, no translucency): every piece of text
    // sits on its own solid box. Persistent text is stacked in two side columns: player stats on
    // the left (Line), plan/objective/blessings/toasts on the right (SideLine). Modals (shop,
    // pause, blessing pick, start menu, victory) use plain GUI.Window on the same skin.
    public sealed class Hud : MonoBehaviour
    {
        struct Entry { public int order; public Action draw; }
        struct ToastEntry { public string text; public float expires; }

        static readonly List<Entry> entries = new List<Entry>();
        static readonly List<ToastEntry> toasts = new List<ToastEntry>();
        const float LineHeight = 20f, ColumnWidth = 360f, Margin = 8f, ButtonWidth = 56f;
        static float leftY, rightY;
        static GUIStyle boxed, smallButton;
        static GUISkin skin;
        public static readonly Color PanelColor = new Color32(28, 26, 38, 255);

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
            TextBox(new Rect(Margin, leftY, ColumnWidth, LineHeight), text);
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
            TextBox(new Rect(textRight - ColumnWidth, rightY, ColumnWidth, LineHeight), text, TextAnchor.UpperRight);
            rightY += button != null ? LineHeight + 2f : LineHeight;
            return clicked;
        }

        // Text on an opaque box hugging it, placed at `anchor` inside rect (rect only positions,
        // the box is as wide as the text). GUI.color tints the text only (the energy warning uses
        // that), so the box stays the same solid panel.
        public static void TextBox(Rect rect, string text, TextAnchor anchor = TextAnchor.UpperLeft)
        {
            Vector2 size = boxed.CalcSize(new GUIContent(text));
            size.y = Mathf.Max(size.y, LineHeight);
            float x = anchor == TextAnchor.UpperRight || anchor == TextAnchor.LowerRight ? rect.xMax - size.x
                : anchor == TextAnchor.MiddleCenter ? rect.center.x - size.x * .5f : rect.x;
            float y = anchor == TextAnchor.LowerRight ? rect.yMax - size.y
                : anchor == TextAnchor.MiddleCenter ? rect.center.y - size.y * .5f : rect.y;
            Color tint = GUI.color;
            GUI.color = Color.white;
            GUI.contentColor = tint;
            GUI.Label(new Rect(x, y, size.x, size.y), text, boxed);
            GUI.contentColor = Color.white;
            GUI.color = tint;
        }

        // Call from inside a registered draw callback, after GUI layout for the frame started.
        public static void WorldLabel(Vector3 world, string text)
        {
            var camera = GameRoot.Instance != null ? GameRoot.Instance.ViewCamera : null;
            if (camera == null) return;
            Vector3 screen = camera.WorldToScreenPoint(world);
            if (screen.z <= 0f) return;
            TextBox(new Rect(screen.x, Screen.height - screen.y, 0f, 0f), text, TextAnchor.MiddleCenter);
        }

        static void EnsureStyles()
        {
            if (skin != null) return;
            // A copy, so the builtin skin (which outlives play mode in the editor) stays untouched.
            skin = Instantiate(GUI.skin);
            Texture2D panel = Solid(PanelColor), field = Solid(new Color32(14, 13, 20, 255));
            Texture2D button = Solid(new Color32(56, 50, 76, 255)), hover = Solid(new Color32(76, 68, 102, 255)), pressed = Solid(new Color32(40, 36, 56, 255));
            Flat(skin.window, panel, panel, panel);
            Flat(skin.box, panel, panel, panel);
            Flat(skin.button, button, hover, pressed);
            Flat(skin.textField, field, field, field);
            Flat(skin.verticalScrollbar, field, field, field);
            Flat(skin.verticalScrollbarThumb, button, hover, pressed);
            Flat(skin.horizontalScrollbar, field, field, field);
            Flat(skin.horizontalScrollbarThumb, button, hover, pressed);
            foreach (var style in new[] { skin.label, skin.window, skin.box, skin.button, skin.textField })
                style.normal.textColor = style.hover.textColor = style.active.textColor = style.focused.textColor =
                    style.onNormal.textColor = style.onHover.textColor = style.onActive.textColor = style.onFocused.textColor = Color.white;
            GUI.skin = skin;
            boxed = new GUIStyle(skin.label) { fontSize = 12, wordWrap = false, clipping = TextClipping.Overflow, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(6, 6, 2, 2) };
            boxed.normal.background = panel;
            smallButton = new GUIStyle(skin.button) { fontSize = 11, padding = new RectOffset(2, 2, 1, 1) };
        }

        // One opaque texture for every state, no 9-slice border: kills the default skin's bevel.
        static void Flat(GUIStyle style, Texture2D normal, Texture2D hover, Texture2D active)
        {
            style.normal.background = style.onNormal.background = style.focused.background = style.onFocused.background = normal;
            style.hover.background = style.onHover.background = hover;
            style.active.background = style.onActive.background = active;
            style.border = new RectOffset(0, 0, 0, 0);
        }

        static Texture2D Solid(Color color)
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
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
