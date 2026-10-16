using UnityEngine;

namespace SoulBlocks
{
    // The special blocks shop (opened from the tree altar) and the bottom block bar.
    public sealed partial class SpecialBlockSystem
    {
        bool shopOpen;
        int shopOpenedFrame = -1, shopClosedFrame = -1;
        public bool HandledInteractionThisFrame => shopOpenedFrame == Time.frameCount || shopClosedFrame == Time.frameCount;
        float previousTimeScale = 1;
        public bool ShopOpen => shopOpen;
        static readonly string[] ActiveDescriptions =
        {
            "Luz en área: revive árboles que atraen y generan almas.", "Pulsos sin daño: preparan cristales para una recompensa extra.",
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
                case SpecialBlock.Yellow:
                    return "Pasiva: al golpear puedes llevar luz durante 5 s. Acércate a los árboles grises (3 m).";
                case SpecialBlock.Blue:
                    return "Pasiva: al picar puede aparecer un aura azul. Al romper: 50% de obtener más fragmentos.";
                case SpecialBlock.Green:
                    return "Pasiva: cada " + BlockRules.HitInterval(Level(block)) + " golpes; mejora de velocidad por " + BlockRules.BuffSeconds(Level(block)).ToString("0.0") + " s.";
                case SpecialBlock.Cyan:
                    return "Pasiva: cada " + BlockRules.HitInterval(Level(block)) + " golpes; ayudante temporal.";
                default:
                    return "Pasiva: explosión por golpe; daño de ayudantes al 50%.";
            }
        }

        string Values(SpecialBlock block)
        {
            int level = Level(block);
            if (block == SpecialBlock.Red) return "Daño " + BlockRules.RedDamage(level) + " · radio " + BlockRules.Radius(block, level).ToString("0.0") + " m";
            if (block == SpecialBlock.Cyan)
                return BlockRules.HelperCount(level) + " ayudantes · " + BlockRules.Duration(block, level) + " s (máximo 4 activos)";
            if (block == SpecialBlock.Blue) return "Radio " + BlockRules.Radius(block, level) + " m · " + BlockRules.Duration(block, level) + " s · pulso cada 1 s · recompensa extra +" + (50 + 25 * (level - 1)) + "%";
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
                if (TreeUpgradeAltar.Instance != null) width = Mathf.Min(660, Screen.width * .62f);
                float windowX = TreeUpgradeAltar.Instance != null ? Screen.width - width - 10 : (Screen.width - width) / 2;
                GUI.Window(947115, new Rect(windowX, (Screen.height - height) / 2, width, height), DrawShop, "ALTAR · MEJORAS DE BLOQUES");
                return;
            }
            if (Keys.Blocked) return;
            if (Time.time < yellowUntil && GameRoot.Instance.Player != null)
                Hud.WorldLabel(GameRoot.Instance.Player.transform.position + Vector3.up * 2.3f,
                    "Luz amarilla · " + (yellowUntil - Time.time).ToString("0.0") + " s · Acércate a un árbol gris");
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
                string label = "[" + (i + 1) + "] " + ((int)Selected == i ? "▶ " : "") + BlockRules.Name(block) + "\nNv " + Level(block)
                    + (cooldowns[i] > 0 ? " · " + cooldowns[i].ToString("0.0") + " s" : " · LISTO");
                GUI.Box(new Rect(x + i * w / 5, y, w / 5 - 4, 42), label);
                GUI.backgroundColor = old;
            }
            GUI.Label(new Rect(x, y + 44, w, 22), "1–5: activar poder · Rueda: ver descripción · E: altar · +/-: zoom");
            GUI.Label(new Rect(x, y + 65, w, 22), ActiveDescriptions[(int)Selected]);
            GUI.Label(new Rect(x, y + 86, w, 22), "5 pasivas activas · Ropa " + Mathf.RoundToInt(BlockRules.Chance(Run.specialBlockClothingLevel) * 100) + "% · Verde " + GreenHits + "/" + BlockRules.HitInterval(Level(SpecialBlock.Green)) + " · Celeste " + CyanHits + "/" + BlockRules.HitInterval(Level(SpecialBlock.Cyan)) + " · Ayudantes " + HelperCount);
            GUI.Label(new Rect(Screen.width / 2f - 5, Screen.height / 2f - 10, 20, 20), "+");
        }

        Vector2 shopScroll;
        void DrawShop(int id)
        {
            GUILayout.Label("Las cinco pasivas funcionan siempre. Mejoras de bloques gratuitas; el pico cuesta cristales.");
            shopScroll = GUILayout.BeginScrollView(shopScroll);
            GUILayout.BeginVertical("box");
            GUILayout.Label("TALENTOS · Almas disponibles: " + Run.reserveSouls);
            GUILayout.Label("Velocidad: " + (GameRoot.Instance.Player.moveSpeed * GameRoot.Instance.Player.SpeedMultiplier).ToString("0.00") + " m/s · Talento nivel " + Run.UpgradeLevel("velocidad"));
            if (GUILayout.Button("Mejorar velocidad en el árbol de talentos"))
            {
                CloseShop();
                GameRoot.Instance.GetComponent<ProgressionSystem>().OpenShop("velocidad");
            }
            GUILayout.EndVertical();
            var mining = GameRoot.Instance.Player.GetComponent<CrystalMiningAttack>();
            if (mining != null)
            {
                GUILayout.BeginVertical("box");
                GUILayout.Label("PICO · Nivel " + mining.Level + "/50 · " + BlockRules.Name((SpecialBlock)MiningBalance.Stage(mining.Level)));
                GUILayout.Label("Daño " + mining.Damage + " · " + mining.Duration.ToString("0.00") + " s por golpe · Cristales: " + Run.collectedCrystals);
                int next = Mathf.Min(50, mining.Level + 1);
                if (mining.Level < 50) GUILayout.Label("Siguiente: " + MiningBalance.Current.Damage(next) + " daño · " + MiningBalance.Current.Seconds(next).ToString("0.00") + " s por golpe");
                int cost = MiningBalance.Current.Cost(mining.Level);
                GUI.enabled = mining.Level < 50 && Run.collectedCrystals >= cost && !mining.IsSwinging;
                if (GUILayout.Button(mining.Level >= 50 ? "Pico al máximo" : "Mejorar pico · " + cost + " cristales")) mining.UpgradePickaxe();
                GUI.enabled = true;
                GUILayout.EndVertical();
            }
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
            GUILayout.Label("Las mejoras no cambian el porcentaje; sólo la ropa.");
            GUILayout.EndScrollView();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Restaurar cristales de prueba")) ResetLab();
            if (GUILayout.Button("Talentos del árbol"))
            { CloseShop(); GameRoot.Instance.GetComponent<ProgressionSystem>().OpenShop(); }
            if (GUILayout.Button("Volver al juego (E / Esc)")) CloseShop();
            GUILayout.EndHorizontal();
        }
    }
}
