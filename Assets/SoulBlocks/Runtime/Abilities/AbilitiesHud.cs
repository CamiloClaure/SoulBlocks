using System.Collections.Generic;
using UnityEngine;

namespace SoulBlocks
{
    // One left-column line for dash, magnet and pick, order 300.
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

            var pick = root.GetComponent<PickAbility>();
            if (pick != null) parts.Add("Pico nv " + root.Stats.GetInt(StatId.PickLevel) + (pick.CooldownRemaining > 0f ? "" : " listo"));

            if (parts.Count > 0) Hud.Line(string.Join("  ·  ", parts));
        }
    }
}
