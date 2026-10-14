using System.Collections.Generic;
using UnityEngine;

namespace SoulBlocks
{
    // One left-column line for the three abilities, order 300.
    public sealed class AbilitiesHud : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() => GameRoot.Register<AbilitiesHud>();

        void OnEnable() => Hud.Register(300, Draw);
        void OnDisable() => Hud.Unregister(Draw);

        void Draw()
        {
            var root = GameRoot.Instance;
            if (root == null) return;
            var parts = new List<string>(3);

            var dash = root.GetComponent<DashAbility>();
            if (dash != null) parts.Add("Dash " + dash.Charges + "/" + dash.MaxCharges);

            var magnet = root.GetComponent<MagnetAbility>();
            if (magnet != null)
                parts.Add("Imán " + (magnet.Active ? "activo"
                    : magnet.CooldownRemaining > 0f ? magnet.CooldownRemaining.ToString("F0") + "s"
                    : "listo"));

            var ray = root.GetComponent<RayhitAbility>();
            if (ray != null) parts.Add("Rayhit " + ray.Charges + "/" + ray.MaxCharges + " (" + ray.SoulsTowardCharge + "/" + ray.SoulsPerCharge + ")");

            if (parts.Count > 0) Hud.Line(string.Join("  ·  ", parts));
            Hud.Line("Picar: mantener click izquierdo · Rayhit: mantener R y soltar");
        }
    }
}
