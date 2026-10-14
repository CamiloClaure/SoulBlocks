using UnityEngine;
using UnityEngine.InputSystem;

namespace SoulBlocks
{
    // Shop at the tree: world prompt when close and not on an expedition, IMGUI window on E.
    // Owns Run.upgrades, the Wisdom-node unlocks, treeStage and the "upgrade.<id>" stat sources.
    public sealed class ProgressionSystem : MonoBehaviour
    {
        bool open;
        Vector2 scrollSouls, scrollWisdom;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() => GameRoot.Register<ProgressionSystem>();

        void OnEnable()
        {
            GameEvents.MapBuilt += OnMapBuilt;
            Hud.Register(400, Draw);
        }

        void OnDisable()
        {
            GameEvents.MapBuilt -= OnMapBuilt;
            Hud.Unregister(Draw);
            if (open) Close(); // ponytail: scene can reload mid-shop, static Keys.Blocked must not stick
        }

        void OnMapBuilt(MapLayout layout)
        {
            var root = GameRoot.Instance;
            ProgressionRules.ApplyStats(root.Run, root.Stats);
            // Defensive: keep treeStage consistent with whatever core nodes a loaded save already owns.
            foreach (var def in UpgradeCatalog.All)
                if (def.core && def.setsTreeStage > root.Run.treeStage && root.Run.UpgradeLevel(def.id) > 0)
                    root.Run.treeStage = def.setsTreeStage;
            GameEvents.RaiseStatsChanged();
        }

        bool NearTree()
        {
            var root = GameRoot.Instance;
            if (root == null || root.Run == null || root.Player == null || root.Tree == null) return false;
            if (root.Run.inExpedition) return false;
            return Vector3.Distance(root.Player.transform.position, root.Tree.position) <= Balance.HubPickupTreeRange;
        }

        void Update()
        {
            if (GameRoot.Instance == null) return;

            if (!open)
            {
                if (NearTree() && Keys.InteractPressed)
                {
                    if (SpecialBlockSystem.Instance != null && SpecialBlockSystem.Instance.Ready) SpecialBlockSystem.Instance.OpenShop();
                    else OpenShop();
                }
                return;
            }

            // Keys.InteractPressed/PausePressed read false while Blocked (which this shop sets),
            // so closing reads the keyboard directly.
            var keyboard = Keyboard.current;
            bool closePressed = keyboard != null && (keyboard.eKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame);
            if (closePressed || !NearTree()) Close();
        }

        public void OpenShop()
        {
            open = true;
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
            var root = GameRoot.Instance;
            if (root == null || root.Run == null) return;

            if (!open)
            {
                if (NearTree() && root.Tree != null) Hud.WorldLabel(root.Tree.position + Vector3.up * 2.5f, "E: Árbol");
                return;
            }

            // ponytail: fixed centered rect redrawn every frame, so a drag snaps back instead of
            // sticking. Fine for a placeholder shop; store the returned Rect if dragging matters later.
            var rect = new Rect(Screen.width * .5f - 340, Screen.height * .5f - 260, 680, 500);
            GUI.Window(947001, rect, DrawWindow, "Árboles de mejoras");
        }

        void DrawWindow(int windowId)
        {
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.Width(320));
            GUILayout.Label("Árbol de Almas");
            scrollSouls = GUILayout.BeginScrollView(scrollSouls, GUILayout.Height(420));
            foreach (var def in UpgradeCatalog.Tree(UpgradeTree.Souls)) DrawRow(def);
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUILayout.Width(320));
            GUILayout.Label("Árbol de Wisdom");
            scrollWisdom = GUILayout.BeginScrollView(scrollWisdom, GUILayout.Height(420));
            foreach (var def in UpgradeCatalog.Tree(UpgradeTree.Wisdom)) DrawRow(def);
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.Label("E / Esc para cerrar");
        }

        void DrawRow(UpgradeDef def)
        {
            var root = GameRoot.Instance;
            var run = root.Run;
            int level = run.UpgradeLevel(def.id);
            string why = ProgressionRules.WhyCannotBuy(run, def);

            GUILayout.BeginVertical("box");
            GUILayout.Label((def.core ? "[Nucleo] " : "") + def.name + "  nv " + level + "/" + def.maxLevel);
            if (level < def.maxLevel)
            {
                string currencyName = def.currency == Currency.Souls ? "almas" : "Wisdom";
                GUILayout.Label("Costo: " + ProgressionRules.Cost(def, level + 1) + " " + currencyName);
                GUILayout.Label("Beneficio: " + ProgressionRules.BenefitEstimate(def, run, root.Stats));
            }
            GUI.enabled = why == null;
            if (GUILayout.Button(why ?? "Comprar")) TryBuy(def.id);
            GUI.enabled = true;
            GUILayout.EndVertical();
        }

        // Public for bots/PlayMode tests: buy by id, bypassing the IMGUI window entirely.
        public bool TryBuy(string id)
        {
            var root = GameRoot.Instance;
            var def = UpgradeCatalog.Get(id);
            if (def == null || root == null || root.Run == null || root.Stats == null) return false;
            if (!ProgressionRules.Buy(root.Run, def)) return false;

            int level = root.Run.UpgradeLevel(id);
            ProgressionRules.ApplyStats(root.Run, root.Stats);

            GameEvents.RaiseUpgradePurchased(id, level);
            if (!string.IsNullOrEmpty(def.unlockId)) GameEvents.RaiseUnlockGranted(def.unlockId);
            if (def.core) GameEvents.RaiseTreeStageChanged(root.Run.treeStage);
            GameEvents.RaiseStatsChanged();

            bool milestone = def.milestoneEvery > 0 && def.milestoneEffects.Count > 0 && level % def.milestoneEvery == 0;
            Hud.Toast(milestone ? "Hito: " + def.milestoneText : "Comprado: " + def.name);
            return true;
        }
    }
}
