using UnityEngine;
using UnityEngine.InputSystem;

namespace SoulBlocks
{
    // Shop at the tree: world prompt when close and not on an expedition, and on E an
    // Albion-style destiny board: a full-screen opaque window (the HUD's scrim and this window
    // hide the world), every upgrade a node around the tree (UpgradeDef.boardPos) linked to the
    // parent it needs. It opens framed on the whole board; drag pans, the wheel zooms, a click on
    // a node shows its details and the buy button. Owns Run.upgrades, the Wisdom-node unlocks, treeStage and the
    // "upgrade.<id>" stat sources.
    public sealed class ProgressionSystem : MonoBehaviour
    {
        const float CellX = 150f, CellY = 84f, NodeWidth = 124f, NodeHeight = 50f, SideWidth = 300f, MinZoom = .7f, MaxZoom = 1.6f;
        static readonly Color EdgeOpen = new Color(.85f, .8f, 1f), EdgeLocked = new Color(.32f, .3f, .4f), Selection = new Color(1f, .85f, .35f);

        bool open;
        Vector2 pan;
        float zoom = 1f;
        string selected;
        static GUIStyle maxedNode, openNode, buyableNode, lockedNode, hubNode; // static: built once, not per scene load

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
            FrameBoard();
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

            // A window, not a plain panel, so it draws above every later Hud line. Fixed rect
            // redrawn every frame (no GUI.DragWindow): the board pans inside it instead.
            GUI.Window(947001, new Rect(0f, 0f, Screen.width, Screen.height), DrawBoard, "Árbol");
        }

        static Rect BoardArea() => new Rect(8f, 48f, Screen.width - SideWidth - 24f, Screen.height - 56f);

        // Center the bounds of every node (and the hub) and zoom so they fit, kept readable.
        void FrameBoard()
        {
            Vector2 min = Vector2.zero, max = Vector2.zero;
            foreach (var def in UpgradeCatalog.All)
            {
                var point = new Vector2(def.boardPos.x * CellX, -def.boardPos.y * CellY);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }
            Vector2 size = max - min + new Vector2(NodeWidth, NodeHeight), area = BoardArea().size;
            zoom = Mathf.Clamp(Mathf.Min(area.x / size.x, area.y / size.y), MinZoom, 1.2f);
            pan = -(min + max) * .5f * zoom;
        }

        void DrawBoard(int windowId)
        {
            EnsureStyles();
            var root = GameRoot.Instance;
            var run = root.Run;
            float width = Screen.width, height = Screen.height;
            GUI.Label(new Rect(12f, 22f, width - 24f, 22f), "Almas " + run.reserveSouls + "   ·   Wisdom " + run.wisdom
                + "        arrastrá para mover · rueda para zoom · click en un nodo · E / Esc cierra");

            var area = BoardArea();
            HandleBoardInput(area);

            GUI.BeginGroup(area);
            Vector2 origin = area.size * .5f + pan;
            Vector2 Center(Vector2 cell) => origin + new Vector2(cell.x * CellX, -cell.y * CellY) * zoom;

            foreach (var def in UpgradeCatalog.All)
            {
                Vector2 from = string.IsNullOrEmpty(def.requires) ? origin : Center(UpgradeCatalog.Get(def.requires).boardPos);
                DrawLine(from, Center(def.boardPos), ProgressionRules.RequirementMet(run, def) ? EdgeOpen : EdgeLocked, 3f * zoom);
            }

            var size = new Vector2(NodeWidth, NodeHeight) * zoom;
            GUI.Box(new Rect(origin - size * .5f, size), "Árbol", hubNode);
            foreach (var def in UpgradeCatalog.All)
            {
                var rect = new Rect(Center(def.boardPos) - size * .5f, size);
                if (def.id == selected)
                {
                    GUI.color = Selection;
                    GUI.DrawTexture(new Rect(rect.x - 3f, rect.y - 3f, rect.width + 6f, rect.height + 6f), Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }
                int level = run.UpgradeLevel(def.id);
                var style = level >= def.maxLevel ? maxedNode
                    : ProgressionRules.WhyCannotBuy(run, def) == null ? buyableNode
                    : ProgressionRules.RequirementMet(run, def) ? openNode : lockedNode;
                if (GUI.Button(rect, def.name + "\n" + level + "/" + def.maxLevel, style)) selected = def.id;
            }
            GUI.EndGroup();

            DrawDetails(new Rect(width - SideWidth - 8f, 48f, SideWidth, height - 56f));
        }

        // Drag pans, the wheel zooms around the cursor; both only inside the board area.
        void HandleBoardInput(Rect area)
        {
            var e = Event.current;
            if (!area.Contains(e.mousePosition)) return;
            if (e.type == EventType.MouseDrag && e.button == 0)
            {
                pan += e.delta;
                e.Use();
            }
            else if (e.type == EventType.ScrollWheel)
            {
                float next = Mathf.Clamp(zoom * (1f - e.delta.y * .05f), MinZoom, MaxZoom);
                Vector2 cursor = e.mousePosition - area.position - area.size * .5f;
                pan = cursor - (cursor - pan) * (next / zoom);
                zoom = next;
                e.Use();
            }
        }

        void DrawDetails(Rect rect)
        {
            GUILayout.BeginArea(rect, GUI.skin.box);
            var def = selected != null ? UpgradeCatalog.Get(selected) : null;
            if (def == null)
            {
                GUILayout.Label("Elegí un nodo del tablero.\n\nLos nodos se abren desde el árbol hacia afuera: cada uno necesita que su padre llegue a cierto nivel.\n\nVerde: se puede comprar. Violeta: abierto, falta moneda u otro requisito. Gris: bloqueado. Dorado: nivel máximo.");
                GUILayout.EndArea();
                return;
            }

            var root = GameRoot.Instance;
            var run = root.Run;
            int level = run.UpgradeLevel(def.id);
            GUILayout.Label((def.core ? "[Núcleo] " : "") + def.name + "   nv " + level + "/" + def.maxLevel);
            GUILayout.Space(6f);
            GUILayout.Label(def.description);
            GUILayout.Space(6f);
            if (!string.IsNullOrEmpty(def.requires))
                GUILayout.Label("Requiere: " + UpgradeCatalog.Get(def.requires).name + " nv " + def.requiresLevel);
            if (level < def.maxLevel)
            {
                string currencyName = def.currency == Currency.Souls ? "almas" : "Wisdom";
                GUILayout.Label("Costo: " + ProgressionRules.Cost(def, level + 1) + " " + currencyName);
                GUILayout.Label("Beneficio: " + ProgressionRules.BenefitEstimate(def, run, root.Stats));
            }
            GUILayout.Space(10f);
            string why = ProgressionRules.WhyCannotBuy(run, def);
            GUI.enabled = why == null;
            if (GUILayout.Button(why ?? "Comprar", GUILayout.Height(32f))) TryBuy(def.id);
            GUI.enabled = true;
            GUILayout.EndArea();
        }

        // A 1 px texture stretched along the segment and rotated around its start.
        static void DrawLine(Vector2 from, Vector2 to, Color color, float width)
        {
            Vector2 delta = to - from;
            if (delta.sqrMagnitude < 1f) return;
            var matrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, from);
            GUI.color = color;
            GUI.DrawTexture(new Rect(from.x, from.y - width * .5f, delta.magnitude, width), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.matrix = matrix;
        }

        void EnsureStyles()
        {
            int fontSize = Mathf.Max(9, Mathf.RoundToInt(13f * zoom));
            if (maxedNode == null)
            {
                GUIStyle Node(Color32 fill, Color32 hover, Color text)
                {
                    var style = new GUIStyle(GUI.skin.button) { wordWrap = true, alignment = TextAnchor.MiddleCenter };
                    style.normal.background = style.focused.background = Hud.Solid(fill);
                    style.hover.background = style.active.background = Hud.Solid(hover);
                    style.normal.textColor = style.hover.textColor = style.active.textColor = style.focused.textColor = text;
                    return style;
                }
                maxedNode = Node(new Color32(128, 98, 30, 255), new Color32(150, 118, 44, 255), Color.white);
                openNode = Node(new Color32(70, 60, 120, 255), new Color32(88, 76, 146, 255), Color.white);
                buyableNode = Node(new Color32(38, 112, 64, 255), new Color32(50, 138, 80, 255), Color.white);
                lockedNode = Node(new Color32(38, 36, 48, 255), new Color32(50, 47, 62, 255), new Color(.6f, .58f, .68f));
                hubNode = Node(new Color32(24, 70, 70, 255), new Color32(24, 70, 70, 255), Color.white);
            }
            maxedNode.fontSize = openNode.fontSize = buyableNode.fontSize = lockedNode.fontSize = hubNode.fontSize = fontSize;
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
