using System.Collections.Generic;
using UnityEngine;

namespace SoulBlocks
{
    // One left-column line for the dash and the pickaxe, order 300.
    public sealed class AbilitiesHud : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() => GameRoot.Register<AbilitiesHud>();

        void OnEnable() => Hud.Register(300, Draw);
        void OnDisable() => Hud.Unregister(Draw);

        void Draw()
        {
            var root = GameRoot.Instance;
            if (root == null || root.Run == null) return;
            var parts = new List<string>(2);

            var dash = root.GetComponent<DashAbility>();
            if (dash != null) parts.Add("Dash " + dash.Charges + "/" + dash.MaxCharges);
            int level = Mathf.Clamp(root.Run.pickaxeLevel, 1, 50);
            parts.Add("Pico nv " + level + " (" + PrisonRules.PickaxeTierName(PrisonRules.PickaxeTier(level)) + ")");

            Hud.Line(string.Join("  ·  ", parts));
            Hud.Line("Picar: mantener click izquierdo");
        }
    }
}
