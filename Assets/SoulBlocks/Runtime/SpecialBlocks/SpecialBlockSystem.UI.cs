using UnityEngine;

namespace SoulBlocks
{
    public sealed partial class SpecialBlockSystem
    {
        bool shopOpen;
        int shopOpenedFrame = -1, shopClosedFrame = -1;
        public bool HandledInteractionThisFrame => shopOpenedFrame == Time.frameCount || shopClosedFrame == Time.frameCount;
        float previousTimeScale = 1;
        public bool ShopOpen => shopOpen;
        static readonly string[] ActiveDescriptions =
        {
            "Ilumina caminos oscuros en una zona.", "Invierte colores y revela cristales ocultos.",
            "Zona de velocidad: movimiento, picado y recargas.", "Invoca ayudantes temporales que recogen y entregan almas.",
            "Coloca una bomba con cuenta atrás visible de 2,5 s."
        };

        public void OpenShop()
        {
            if (!Ready || shopOpen) return;
            shopOpen = true;
            shopOpenedFrame = Time.frameCount;
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0;
            Keys.Blocked = true;
            if (GameRoot.Instance.Player != null) GameRoot.Instance.Player.MovementLocked = true;
        }
        public void CloseShop()
        {
            shopClosedFrame = Time.frameCount;
            shopOpen = false;
            Time.timeScale = previousTimeScale;
            Keys.Blocked = false;
            if (GameRoot.Instance != null && GameRoot.Instance.Player != null) GameRoot.Instance.Player.MovementLocked = false;
        }
        public bool Upgrade(SpecialBlock block)
        {
            if (!Ready || Level(block) >= BlockRules.MaxLevel) return false;
            Run.specialBlockLevels[(int)block]++;
            return true;
        }
        public bool UpgradeClothing()
        {
            if (!Ready || Run.specialBlockClothingLevel >= BlockRules.MaxLevel) return false;
            Run.specialBlockClothingLevel++;
            return true;
        }
        string Passive(SpecialBlock block)
        {
            switch (block)
            {
                case SpecialBlock.Yellow: return "Pasiva: tirada cada 1,5 m caminados.";
                case SpecialBlock.Blue: return "Pasiva: tirada al iluminar un tramo oscuro.";
                case SpecialBlock.Green: return "Pasiva: cada " + BlockRules.HitInterval(Level(block)) + " golpes; mejora de velocidad por " + BlockRules.BuffSeconds(Level(block)).ToString("0.0") + " s.";
                case SpecialBlock.Celeste: return "Pasiva: cada " + BlockRules.HitInterval(Level(block)) + " golpes; ayudante temporal.";
                default: return "Pasiva: explosión por golpe; daño de ayudantes al 50%.";
            }
        }
        string Values(SpecialBlock block)
        {
            int level = Level(block);
            if (block == SpecialBlock.Red) return "Daño " + BlockRules.RedDamage(level) + " · radio " + BlockRules.Radius(block, level).ToString("0.0") + " m";
            if (block == SpecialBlock.Celeste) return BlockRules.HelperCount(level) + " ayudantes · " + BlockRules.Duration(block, level) + " s (máximo 4 activos)";
            return "Radio " + BlockRules.Radius(block, level) + " m · " + BlockRules.Duration(block, level) + " s"
                + (block == SpecialBlock.Green ? " · velocidad ×" + BlockRules.Speed(level).ToString("0.00") : "");
        }

        void Draw()
        {
            if (!Ready) return;
            if (shopOpen)
            {
                float width = Mathf.Min(820, Screen.width - 20);
                float height = Mathf.Min(640, Screen.height - 20);
                GUI.Window(947115, new Rect((Screen.width - width) / 2, (Screen.height - height) / 2, width, height), DrawShop, "ÁRBOL · BLOQUES ESPECIALES · ZONA DE PRUEBA");
                return;
            }
            if (Keys.Blocked) return;
            // Countdown over each bomb, boxed like every world label (Hud).
            foreach (var bomb in bombs) Hud.WorldLabel(bomb.position + Vector3.up * 1.4f, Mathf.Max(0f, bomb.until - Time.time).ToString("0.0") + " s");
            float w = Mathf.Min(600, Screen.width - 24);
            float x = (Screen.width - w) / 2;
            float y = Screen.height - 134;
            GUI.Box(new Rect(x - 8, y - 8, w + 16, 132), "");
            for (int i = 0; i < 5; i++)
            {
                var block = (SpecialBlock)i;
                var old = GUI.backgroundColor;
                GUI.backgroundColor = BlockRules.ColorOf(block);
                string label = ((int)Selected == i ? "▶ " : "") + BlockRules.Name(block) + "\nNv " + Level(block)
                    + (cooldowns[i] > 0 ? " · " + cooldowns[i].ToString("0.0") + " s" : " · LISTO");
                GUI.Box(new Rect(x + i * w / 5, y, w / 5 - 4, 42), label);
                GUI.backgroundColor = old;
            }
            GUI.Label(new Rect(x, y + 44, w, 22), "Rueda: elegir · Click derecho: usar · E en el árbol: mejorar · +/-: zoom");
            GUI.Label(new Rect(x, y + 65, w, 22), ActiveDescriptions[(int)Selected]);
            GUI.Label(new Rect(x, y + 86, w, 22), "5 pasivas activas · Ropa " + Mathf.RoundToInt(BlockRules.Chance(Run.specialBlockClothingLevel) * 100) + "% · Verde " + GreenHits + "/" + BlockRules.HitInterval(Level(SpecialBlock.Green)) + " · Celeste " + CelesteHits + "/" + BlockRules.HitInterval(Level(SpecialBlock.Celeste)) + " · Ayudantes " + HelperCount);
            GUI.Label(new Rect(Screen.width / 2f - 5, Screen.height / 2f - 10, 20, 20), "+");
        }
        Vector2 shopScroll;
        void DrawShop(int id)
        {
            GUILayout.Label("Las cinco pasivas funcionan siempre. Mejoras gratuitas durante esta prueba.");
            shopScroll = GUILayout.BeginScrollView(shopScroll);
            for (int i = 0; i < 5; i++)
            {
                var block = (SpecialBlock)i;
                GUILayout.BeginVertical("box");
                GUILayout.BeginHorizontal();
                var old = GUI.color;
                GUI.color = BlockRules.ColorOf(block);
                GUILayout.Label(BlockRules.Name(block).ToUpperInvariant() + " · Nivel " + Level(block) + "/5");
                GUI.color = old;
                GUI.enabled = Level(block) < BlockRules.MaxLevel;
                if (GUILayout.Button("Mejorar", GUILayout.Width(110))) Upgrade(block);
                GUI.enabled = true;
                GUILayout.EndHorizontal();
                GUILayout.Label(ActiveDescriptions[i] + " " + Values(block));
                GUILayout.Label(Passive(block));
                GUILayout.EndVertical();
            }
            GUILayout.BeginHorizontal("box");
            GUILayout.Label("ROPA · Nivel " + Run.specialBlockClothingLevel + "/5 · Probabilidad de todas las pasivas: " + Mathf.RoundToInt(BlockRules.Chance(Run.specialBlockClothingLevel) * 100) + "%");
            GUI.enabled = Run.specialBlockClothingLevel < BlockRules.MaxLevel;
            if (GUILayout.Button("Mejorar ropa", GUILayout.Width(110))) UpgradeClothing();
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.Label("Almas entregadas por ayudantes: " + DeliveredSouls + " · Las mejoras no cambian el porcentaje; sólo la ropa.");
            GUILayout.EndScrollView();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Restaurar los 15 cristales")) ResetLab();
            if (GUILayout.Button("Mejoras originales del árbol")) { CloseShop(); GameRoot.Instance.GetComponent<ProgressionSystem>().OpenShop(); }
            if (GUILayout.Button("Volver al juego (E / Esc)")) CloseShop();
            GUILayout.EndHorizontal();
        }
    }
}
