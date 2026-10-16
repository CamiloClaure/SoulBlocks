using UnityEngine;

namespace SoulBlocks
{
    // First-boot modal: continue a save or start a new game (the run GameRoot already booted is
    // a fresh one). Shown once per process (static flag reset only at SubsystemRegistration),
    // never again after a GameRoot.Restart within the same run.
    public sealed class StartMenu : MonoBehaviour
    {
        static bool shownThisProcess;
        bool open;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() => GameRoot.Register<StartMenu>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => shownThisProcess = false;

        // Tests run many scene loads in one play session: lets a test see the "first boot" menu again.
        public static void ResetForTests() => shownThisProcess = false;

        void OnEnable() { GameEvents.MapBuilt += OnMapBuilt; Hud.Register(800, Draw); }

        void OnDisable()
        {
            GameEvents.MapBuilt -= OnMapBuilt;
            Hud.Unregister(Draw);
            if (open) CloseInternal(); // scene can reload mid-modal; static Keys.Blocked must not stick
        }

        void OnMapBuilt(MapLayout layout)
        {
            if (shownThisProcess) return;
            shownThisProcess = true;
            Open();
        }

        void Open()
        {
            open = true;
            Keys.Blocked = true;
            if (GameRoot.Instance.Player != null) GameRoot.Instance.Player.MovementLocked = true;
        }

        // Public for bots/tests and the "Nueva partida" button: close without restarting,
        // keeping the fresh run GameRoot already built.
        public static void Dismiss()
        {
            var menu = GameRoot.Instance != null ? GameRoot.Instance.GetComponent<StartMenu>() : null;
            if (menu != null) menu.CloseInternal();
        }

        void CloseInternal()
        {
            open = false;
            Keys.Blocked = false;
            if (GameRoot.Instance != null && GameRoot.Instance.Player != null) GameRoot.Instance.Player.MovementLocked = false;
        }

        void Draw()
        {
            if (!open) return;
            var rect = new Rect(Screen.width * .5f - 180, Screen.height * .5f - 70, 360, 140);
            GUI.Window(947101, rect, DrawWindow, "SoulBlocks");
        }

        void DrawWindow(int windowId)
        {
            GUI.enabled = SaveSystem.HasValidSave();
            if (GUILayout.Button("Continuar")) GameRoot.Restart(SaveSystem.TryLoad());
            GUI.enabled = true;
            if (GUILayout.Button("Nueva partida")) CloseInternal();
        }
    }
}
