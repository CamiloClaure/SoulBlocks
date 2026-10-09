using UnityEngine;
using UnityEngine.InputSystem;

namespace SoulBlocks
{
    // Escape-triggered pause: continue, save, controls, quit; below that, restart with a seed
    // and copy the seed. Freezes time (energy must not drain while it is open). Never opens
    // while another modal (shop, blessing, the start menu) already holds Keys.Blocked, because
    // Keys.PausePressed itself reads false whenever Blocked is true.
    public sealed class PauseMenu : MonoBehaviour
    {
        // Same table as README.md "Jugar".
        static readonly string[] Controls =
        {
            "WASD: moverse (la cámara se acomoda detrás)",
            "Rueda: zoom de la cámara",
            "Shift: dash",
            "Q: imán de almas",
            "Click izquierdo: mantener para apuntar Rayhit, soltar para lanzar",
            "E: interactuar (tienda del árbol, altares, entrega en refugios)",
            "Tab (mantener): mapa",
            "Esc: pausa",
        };

        bool open, showControls;
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
            showControls = false;
            seedText = GameRoot.Instance.Run.seed.ToString();
            Time.timeScale = 0f;
            Keys.Blocked = true;
            if (GameRoot.Instance.Player != null) GameRoot.Instance.Player.MovementLocked = true;
        }

        void Close()
        {
            open = false;
            Time.timeScale = 1f;
            Keys.Blocked = false;
            if (GameRoot.Instance != null && GameRoot.Instance.Player != null) GameRoot.Instance.Player.MovementLocked = false;
        }

        void Draw()
        {
            if (!open) return;
            float height = showControls ? 470f : 330f;
            var rect = new Rect(Screen.width * .5f - 220, Screen.height * .5f - height * .5f, 440, height);
            GUI.Window(947102, rect, DrawWindow, "Pausa");
        }

        void DrawWindow(int windowId)
        {
            var run = GameRoot.Instance.Run;
            if (GUILayout.Button("Continuar")) Close();
            if (GUILayout.Button("Guardar")) { SaveSystem.SaveNow(); Hud.Toast("Partida guardada"); }
            if (GUILayout.Button(showControls ? "Ocultar controles" : "Controles")) showControls = !showControls;
            if (showControls)
                foreach (var line in Controls) GUILayout.Label(line);
            if (GUILayout.Button("Salir")) Quit();

            GUILayout.Space(16);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Seed", GUILayout.Width(36));
            seedText = SeedField.Filter(GUILayout.TextField(seedText, GUILayout.Width(120)));
            if (GUILayout.Button("Nueva partida"))
            {
                int seed = SeedField.TryParse(seedText, out int parsed) ? parsed : run.seed;
                Close();
                GameRoot.Restart(seed, null);
            }
            if (GUILayout.Button("Copiar seed")) GUIUtility.systemCopyBuffer = run.effectiveSeed.ToString();
            GUILayout.EndHorizontal();
        }

        // Saves first: quitting from the menu must never lose the run.
        void Quit()
        {
            SaveSystem.SaveNow();
            Close();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
