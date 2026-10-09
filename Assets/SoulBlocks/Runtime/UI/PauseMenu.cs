using UnityEngine;
using UnityEngine.InputSystem;

namespace SoulBlocks
{
    // Escape-triggered pause: continue, save now, restart with a new seed, copy the seed, quit.
    // Never opens while another modal (shop, blessing, the start menu) already holds
    // Keys.Blocked, because Keys.PausePressed itself reads false whenever Blocked is true.
    public sealed class PauseMenu : MonoBehaviour
    {
        bool open;
        string seedText = "";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() => GameRoot.Register<PauseMenu>();

        void OnEnable() => Hud.Register(810, Draw);

        void OnDisable()
        {
            Hud.Unregister(Draw);
            if (open) Close();
        }

        void Update()
        {
            if (GameRoot.Instance == null) return;

            if (!open)
            {
                if (Keys.PausePressed) Open();
                return;
            }

            // Keys.PausePressed reads false while Blocked (which this menu itself sets), so
            // closing reads the keyboard directly, same pattern as ProgressionSystem's shop.
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) Close();
        }

        void Open()
        {
            open = true;
            seedText = GameRoot.Instance.Run.seed.ToString();
            Keys.Blocked = true;
            if (GameRoot.Instance.Player != null) GameRoot.Instance.Player.MovementLocked = true;
        }

        void Close()
        {
            open = false;
            Keys.Blocked = false;
            if (GameRoot.Instance != null && GameRoot.Instance.Player != null) GameRoot.Instance.Player.MovementLocked = false;
        }

        void Draw()
        {
            if (!open) return;
            var rect = new Rect(Screen.width * .5f - 180, Screen.height * .5f - 160, 360, 320);
            GUI.Window(947102, rect, DrawWindow, "Pausa");
        }

        void DrawWindow(int windowId)
        {
            var run = GameRoot.Instance.Run;
            if (GUILayout.Button("Continuar")) Close();
            if (GUILayout.Button("Guardar")) { SaveSystem.SaveNow(); Hud.Toast("Partida guardada"); }

            GUILayout.Space(10);
            GUILayout.Label("Nueva partida (seed):");
            seedText = SeedField.Filter(GUILayout.TextField(seedText));
            if (GUILayout.Button("Nueva partida"))
            {
                int seed = SeedField.TryParse(seedText, out int parsed) ? parsed : run.seed;
                GameRoot.Restart(seed, null);
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Copiar seed")) GUIUtility.systemCopyBuffer = run.effectiveSeed.ToString();
            if (GUILayout.Button("Salir")) Application.Quit();
        }
    }
}
