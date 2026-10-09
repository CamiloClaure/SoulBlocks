using UnityEngine;

namespace SoulBlocks
{
    // Wisdom's "Entrega en refugios" (section 1.7): once unlocked, a shelter lets the player
    // deposit cargo on the spot instead of walking back to the tree. ShelterZone (og/mapa) owns
    // the enter/exit events, ExpeditionController.DepositCargo() owns the actual move to Run.reserveSouls.
    public sealed class ShelterDelivery : MonoBehaviour
    {
        const float LabelRange = 30f;
        int currentShelterId = -1;
        MapLayout layout;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() => GameRoot.Register<ShelterDelivery>();

        void OnEnable()
        {
            GameEvents.MapBuilt += OnMapBuilt;
            GameEvents.ShelterEntered += OnShelterEntered;
            GameEvents.ShelterExited += OnShelterExited;
            Hud.Register(700, DrawPrompt);
        }

        void OnDisable()
        {
            GameEvents.MapBuilt -= OnMapBuilt;
            GameEvents.ShelterEntered -= OnShelterEntered;
            GameEvents.ShelterExited -= OnShelterExited;
            Hud.Unregister(DrawPrompt);
        }

        void OnMapBuilt(MapLayout mapLayout) { layout = mapLayout; currentShelterId = -1; }
        void OnShelterEntered(int id) => currentShelterId = id;
        void OnShelterExited(int id) { if (currentShelterId == id) currentShelterId = -1; }

        bool CanDeliver(RunState run) => currentShelterId >= 0 && run != null && run.HasUnlock(UnlockIds.Entrega) && run.cargoSouls > 0;

        void Update()
        {
            var root = GameRoot.Instance;
            if (root == null || !CanDeliver(root.Run) || !Keys.InteractPressed) return;

            int amount = root.Run.cargoSouls;
            ExpeditionController.Instance?.DepositCargo();
            Hud.Toast("Entregaste " + amount + " almas");
        }

        // "Refugio" over nearby shelters (what the pale disc is), plus the delivery prompt.
        void DrawPrompt()
        {
            var root = GameRoot.Instance;
            if (root == null || root.Player == null) return;
            Vector3 player = root.Player.transform.position;
            if (layout != null)
                foreach (var shelter in layout.shelters)
                    if ((shelter.position - player).sqrMagnitude <= LabelRange * LabelRange)
                        Hud.WorldLabel(shelter.position + Vector3.up * 1.2f, "Refugio");
            if (CanDeliver(root.Run)) Hud.WorldLabel(player + Vector3.up * 2.2f, "E: Entregar carga");
        }
    }
}
