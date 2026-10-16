using UnityEngine;

namespace SoulBlocks
{
    // Opens the victory screen when the Nucleo breaks. FinalRules.EndsRun is the only gate on
    // PrisonBroken (raised by PrisonView for every tier); Run.victory then guards
    // against a loaded save past victory replaying any of this (plan: "no replay the ending").
    public sealed class VictorySystem : MonoBehaviour
    {
        bool open;

        // Public for PlayMode tests and for anything that wants to know without polling GUI state.
        public bool IsOpen => open;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() => GameRoot.Register<VictorySystem>();

        void OnEnable()
        {
            GameEvents.PrisonBroken += OnPrisonBroken;
            Hud.Register(770, DrawTag);
            Hud.Register(780, Draw);
        }

        void OnDisable()
        {
            GameEvents.PrisonBroken -= OnPrisonBroken;
            Hud.Unregister(DrawTag);
            Hud.Unregister(Draw);
            if (open) Continue(); // ponytail: scene can reload mid-screen, static Keys.Blocked must not stick
        }

        void OnPrisonBroken(int prisonId, PrisonTier tier)
        {
            if (!FinalRules.EndsRun(tier)) return;
            var root = GameRoot.Instance;
            if (root == null || root.Run == null || root.Run.victory) return;

            var run = root.Run;
            run.victory = true;
            GameEvents.RaiseGameWon();
            if (ExpeditionController.Instance != null) ExpeditionController.Instance.EndExpedition(ExpeditionEndReason.Final);
            OpenScreen();
        }

        void OpenScreen()
        {
            open = true;
            Keys.Blocked = true;
            var root = GameRoot.Instance;
            if (root != null && root.Player != null) root.Player.MovementLocked = true;
        }

        // "Seguir jugando": public so the IMGUI button and PlayMode tests both close it the
        // same way (keeps the run going, for finishing optional nodes).
        public void Continue()
        {
            open = false;
            Keys.Blocked = false;
            var root = GameRoot.Instance;
            if (root != null && root.Player != null) root.Player.MovementLocked = false;
        }

        void DrawTag()
        {
            var run = GameRoot.Instance != null ? GameRoot.Instance.Run : null;
            if (run == null || !run.victory) return;
            Hud.SideLine("Victoria");
        }

        void Draw()
        {
            if (!open) return;
            var rect = new Rect(Screen.width * .5f - 260, Screen.height * .5f - 220, 520, 440);
            GUI.Window(947002, rect, DrawWindow, "Victoria");
        }

        void DrawWindow(int windowId)
        {
            var root = GameRoot.Instance;
            if (root == null || root.Run == null)
            { GUILayout.Label("..."); return; }

            foreach (var line in RunSummary.Lines(root.Run))
                GUILayout.Label(line);

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Seguir jugando")) Continue();
            if (GUILayout.Button("Nueva partida")) GameRoot.Restart(null);
            GUILayout.EndHorizontal();
        }
    }
}
