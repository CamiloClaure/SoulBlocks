using System.Collections.Generic;
using UnityEngine;

namespace SoulBlocks
{
    // Tab-held top-down overview. Discovered tiles come from Run.discoveredTiles (TileDiscovery
    // writes it, we only read it). With UnlockIds.Revelacion, also draws undiscovered prisons'
    // secondary props and the plan anomaly straight off the layout data, without requiring the
    // player to have walked there.
    public sealed class MapScreen : MonoBehaviour
    {
        static Texture2D pixel;

        MapLayout layout;
        readonly Dictionary<int, TileLayout> tileByIndex = new Dictionary<int, TileLayout>();
        float minX, maxX, minZ, maxZ;
        Rect area;
        float scale;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() => GameRoot.Register<MapScreen>();

        void OnEnable() { GameEvents.MapBuilt += OnMapBuilt; Hud.Register(820, Draw); }
        void OnDisable() { GameEvents.MapBuilt -= OnMapBuilt; Hud.Unregister(Draw); }

        void OnMapBuilt(MapLayout mapLayout)
        {
            layout = mapLayout;
            tileByIndex.Clear();
            foreach (var tile in layout.tiles) tileByIndex[tile.index] = tile;

            minX = maxX = layout.hubCenter.x;
            minZ = maxZ = layout.hubCenter.z;
            Expand(layout.hubCenter);
            foreach (var tile in layout.tiles) Expand(tile.position);
            foreach (var prison in layout.prisons) Expand(prison.position);
        }

        void Expand(Vector3 p)
        {
            if (p.x < minX) minX = p.x;
            if (p.x > maxX) maxX = p.x;
            if (p.z < minZ) minZ = p.z;
            if (p.z > maxZ) maxZ = p.z;
        }

        static Texture2D Pixel()
        {
            if (pixel == null)
            {
                pixel = new Texture2D(1, 1);
                pixel.SetPixel(0, 0, Color.white);
                pixel.Apply();
            }
            return pixel;
        }

        void Draw()
        {
            if (!Keys.MapHeld || layout == null) return;
            var root = GameRoot.Instance;
            if (root == null || root.Run == null) return;
            var run = root.Run;

            float size = Mathf.Min(Screen.width, Screen.height) - Balance.MapScreenMargin * 2f;
            area = new Rect(Screen.width * .5f - size * .5f, Screen.height * .5f - size * .5f, size, size);
            float spanX = Mathf.Max(1f, maxX - minX), spanZ = Mathf.Max(1f, maxZ - minZ);
            scale = Mathf.Min(area.width / spanX, area.height / spanZ) * .9f;

            GUI.color = Hud.PanelColor;
            GUI.DrawTexture(area, Pixel());
            GUI.color = Color.white;

            bool revealed = run.HasUnlock(UnlockIds.Revelacion);
            if (revealed) DrawRevelacion(run);

            DrawMark(ToScreen(layout.hubCenter), 10f, Color.white);

            foreach (var tile in layout.tiles)
            {
                if (!run.discoveredTiles.Contains(tile.index)) continue;
                // The path itself: dots along the ribbon's center, as wide as the ribbon.
                for (int i = 0; i < tile.edgeLeft.Count; i++)
                {
                    Vector3 center = (tile.edgeLeft[i] + tile.edgeRight[i]) * .5f;
                    float width = Vector3.Distance(tile.edgeLeft[i], tile.edgeRight[i]);
                    DrawMark(ToScreen(center), Mathf.Max(3f, width * scale), BranchColor(tile.branchId));
                }
                if (tile.edgeLeft.Count == 0) DrawMark(ToScreen(tile.position), 4f, BranchColor(tile.branchId));
            }

            foreach (var shelter in layout.shelters)
                if (OnDiscoveredTile(run, shelter.position))
                    DrawMark(ToScreen(shelter.position), 5f, new Color(.55f, .95f, 1f, .9f));

            foreach (var prison in layout.prisons)
            {
                bool discovered = NearDiscoveredTile(run, prison.position);
                if (discovered || revealed) DrawPrison(run, prison, discovered);
            }

            foreach (var portal in layout.portals)
                if (PortalActive(run, portal))
                    DrawMark(ToScreen(portal.position), 7f, new Color(.6f, .2f, 1f, .9f));

            if (root.Player != null) DrawMark(ToScreen(root.Player.transform.position), 6f, Color.green);
            GUI.color = Color.white;
        }

        void DrawRevelacion(RunState run)
        {
            if (run.plan != null && run.plan.anomaly != null && !run.plan.anomaly.consumed)
                DrawMark(ToScreen(run.plan.anomaly.position), 8f, new Color(1f, .2f, 1f, .9f));
        }

        void DrawPrison(RunState run, PrisonLayout prison, bool discovered)
        {
            var record = run.Prison(prison.id);
            Color color = record.broken ? new Color(.4f, .4f, .4f, .5f) : TierColor(prison.tier);
            if (!discovered) color.a *= .6f; // revealed-but-unwalked: dimmer than ground actually visited
            var pos = ToScreen(prison.position);
            DrawMark(pos, 6f, color);

            string label = discovered ? TierSymbol(prison.tier) : PropsLabel(prison.props);
            if (string.IsNullOrEmpty(label)) return;
            GUI.color = Color.white;
            Hud.TextBox(new Rect(pos.x, pos.y - 14f, 0f, 0f), label, TextAnchor.MiddleCenter);
        }

        // Same rule PortalSystem activates rings by: Raiz I owned and the branch-end prison broken.
        bool PortalActive(RunState run, PortalLayout portal)
        {
            if (!run.HasUnlock(UnlockIds.Raiz1)) return false;
            foreach (var branch in layout.branches)
                if (branch.id == portal.branchId) return branch.endPrisonId >= 0 && run.Prison(branch.endPrisonId).broken;
            return false;
        }

        bool OnDiscoveredTile(RunState run, Vector3 position)
        {
            float r = Balance.TileSize * .55f;
            foreach (int index in run.discoveredTiles)
            {
                if (!tileByIndex.TryGetValue(index, out var tile)) continue;
                if (FlatDistanceSqr(tile.position, position) <= r * r) return true;
            }
            return false;
        }

        bool NearDiscoveredTile(RunState run, Vector3 position)
        {
            float r = Balance.TileSize * 1.5f;
            foreach (int index in run.discoveredTiles)
            {
                if (!tileByIndex.TryGetValue(index, out var tile)) continue;
                if (FlatDistanceSqr(tile.position, position) <= r * r) return true;
            }
            return false;
        }

        static float FlatDistanceSqr(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x, dz = a.z - b.z;
            return dx * dx + dz * dz;
        }

        Vector2 ToScreen(Vector3 world)
        {
            float x = area.x + area.width * .5f + (world.x - (minX + maxX) * .5f) * scale;
            float y = area.y + area.height * .5f - (world.z - (minZ + maxZ) * .5f) * scale; // +z (north) up, like SoulCamera at its starting yaw
            return new Vector2(x, y);
        }

        static void DrawMark(Vector2 center, float size, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(new Rect(center.x - size * .5f, center.y - size * .5f, size, size), Pixel());
        }

        Color BranchColor(int branchId)
        {
            foreach (var branch in layout.branches)
                if (branch.id == branchId) return SpecialtyColor(branch.specialty);
            return Color.gray; // -1: connection tile between branches
        }

        static Color SpecialtyColor(BranchSpecialty specialty)
        {
            switch (specialty)
            {
                case BranchSpecialty.Souls:
                    return Color.yellow;
                case BranchSpecialty.Prisons:
                    return new Color(.2f, .8f, .2f);
                default:
                    return new Color(.3f, .6f, 1f);
            }
        }

        static Color TierColor(PrisonTier tier)
        {
            switch (tier)
            {
                case PrisonTier.T1:
                    return Color.yellow;
                case PrisonTier.T2:
                    return new Color(.2f, .8f, .2f);
                case PrisonTier.T3:
                    return new Color(.2f, .4f, 1f);
                case PrisonTier.T4:
                    return Color.red;
                default:
                    return Color.white; // Nucleo
            }
        }

        static string TierSymbol(PrisonTier tier)
        {
            switch (tier)
            {
                case PrisonTier.T1:
                    return "I";
                case PrisonTier.T2:
                    return "II";
                case PrisonTier.T3:
                    return "III";
                case PrisonTier.T4:
                    return "IV";
                default:
                    return "N"; // Nucleo
            }
        }

        // R = resonante, N = nucleo expuesto (section 1.5 props). Overlaps on purpose with the
        // Nucleo tier symbol "N": the two never draw in the same context (tier symbol only shows
        // once a prison is actually discovered).
        static string PropsLabel(PrisonProp props)
        {
            string s = "";
            if ((props & PrisonProp.Resonant) != 0) s += "R";
            if ((props & PrisonProp.ExposedCore) != 0) s += "N";
            return s;
        }
    }
}
