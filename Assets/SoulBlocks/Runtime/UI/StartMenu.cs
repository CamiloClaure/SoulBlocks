using UnityEngine;

namespace SoulBlocks
{
    // First-boot modal: continue a save, start a new seed, or keep the world GameRoot already
    // booted. Shown once per process (static flag reset only at SubsystemRegistration), never
    // again after a GameRoot.Restart within the same run.
    public sealed class StartMenu : MonoBehaviour
    {
        static bool shownThisProcess;
        bool open;
        string seedText = "";

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
            if (GameRoot.Instance.ManualWorld) return;
            if (shownThisProcess) return;
            shownThisProcess = true;
            Open();
        }

        void Open()
        {
            open = true;
            seedText = GameRoot.Instance.Run.seed.ToString();
            Keys.Blocked = true;
            if (GameRoot.Instance.Player != null) GameRoot.Instance.Player.MovementLocked = true;
        }

        // Public for bots/tests and the "Jugar con este mundo" button: close without restarting,
        // keeping whatever GameRoot already built.
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
            var rect = new Rect(Screen.width * .5f - 220, Screen.height * .5f - 150, 440, 300);
            GUI.Window(947101, rect, DrawWindow, "SoulBlocks");
        }

        void DrawWindow(int windowId)
        {
            var run = GameRoot.Instance.Run;
            GUILayout.Label("Seed actual: " + run.effectiveSeed);

            GUI.enabled = SaveSystem.HasValidSave();
            if (GUILayout.Button("Continuar")) GameRoot.Restart(null, SaveSystem.TryLoad());
            GUI.enabled = true;

            GUILayout.Space(12);
            GUILayout.Label("Nueva partida (seed):");
            seedText = SeedField.Filter(GUILayout.TextField(seedText));
            if (GUILayout.Button("Nueva partida"))
            {
                int seed = SeedField.TryParse(seedText, out int parsed) ? parsed : run.seed;
                GameRoot.Restart(seed, null);
            }

            GUILayout.Space(12);
            if (GUILayout.Button("Jugar con este mundo")) CloseInternal();
        }
    }
}
