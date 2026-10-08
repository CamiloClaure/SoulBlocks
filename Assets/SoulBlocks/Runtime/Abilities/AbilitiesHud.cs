using UnityEngine;

namespace SoulBlocks
{
    // Single combined panel for this crew's three abilities, order 300.
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
            float y = 30f;

            var dash = root.GetComponent<DashAbility>();
            if (dash != null)
            {
                GUI.Label(new Rect(6, y, 260, 20), "Dash: " + dash.Charges + "/" + dash.MaxCharges + " cargas");
                y += 18f;
            }

            var magnet = root.GetComponent<MagnetAbility>();
            if (magnet != null)
            {
                string status = magnet.Active ? "activo"
                    : magnet.CooldownRemaining > 0f ? "cooldown " + magnet.CooldownRemaining.ToString("F1") + "s"
                    : "listo";
                GUI.Label(new Rect(6, y, 260, 20), "Iman: " + status);
                y += 18f;
            }

            var ray = root.GetComponent<RayhitAbility>();
            if (ray != null)
                GUI.Label(new Rect(6, y, 260, 20), "Rayhit: " + ray.Charges + "/" + ray.MaxCharges + " cargas (almas " + ray.SoulsTowardCharge + "/" + ray.SoulsPerCharge + ")");
        }
    }
}
