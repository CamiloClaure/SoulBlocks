using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SoulBlocks
{
    // Shared IMGUI surface: every panel registers a draw callback instead of adding its own
    // OnGUI, so draw order stays controllable and there is exactly one GUI.skin to agree on.
    public sealed class Hud : MonoBehaviour
    {
        struct Entry { public int order; public Action draw; }
        struct ToastEntry { public string text; public float expires; }

        static readonly List<Entry> entries = new List<Entry>();
        static readonly List<ToastEntry> toasts = new List<ToastEntry>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { if (GameRoot.ShouldBoot) GameRoot.Attach<Hud>(); }

        public static void Register(int order, Action draw) => entries.Add(new Entry { order = order, draw = draw });

        public static void Unregister(Action draw) => entries.RemoveAll(e => e.draw == draw);

        public static void Toast(string text, float seconds = 2f) => toasts.Add(new ToastEntry { text = text, expires = Time.time + seconds });

        // Call from inside a registered draw callback, after GUI layout for the frame started.
        public static void WorldLabel(Vector3 world, string text)
        {
            var camera = GameRoot.Instance != null ? GameRoot.Instance.ViewCamera : null;
            if (camera == null) return;
            Vector3 screen = camera.WorldToScreenPoint(world);
            if (screen.z <= 0f) return;
            float y = Screen.height - screen.y;
            Vector2 size = GUI.skin.label.CalcSize(new GUIContent(text));
            GUI.Label(new Rect(screen.x - size.x * .5f, y - size.y * .5f, size.x, size.y), text);
        }

        void OnGUI()
        {
            foreach (var entry in entries.OrderBy(e => e.order).ToArray())
                entry.draw?.Invoke();

            toasts.RemoveAll(t => t.expires <= Time.time);
            float y = 10f;
            foreach (var toast in toasts)
            {
                Vector2 size = GUI.skin.label.CalcSize(new GUIContent(toast.text));
                GUI.Label(new Rect(Screen.width * .5f - size.x * .5f, y, size.x, size.y), toast.text);
                y += size.y + 2f;
            }

            var run = GameRoot.Instance != null ? GameRoot.Instance.Run : null;
            GUI.Label(new Rect(6, 6, 300, 20), "seed " + (run != null ? run.effectiveSeed.ToString() : "-"));
        }
    }
}
