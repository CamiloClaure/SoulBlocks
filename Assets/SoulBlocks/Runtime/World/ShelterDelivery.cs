using UnityEngine;

namespace SoulBlocks
{
    // Wisdom's "Entrega en refugios" (section 1.7): once unlocked, a shelter lets the player
    // deposit cargo on the spot instead of walking back to the tree. ShelterZone (og/mapa) owns
    // the enter/exit events, ExpeditionController.DepositCargo() owns the actual move to Run.reserveSouls.
    public sealed class ShelterDelivery : MonoBehaviour
    {
        int currentShelterId = -1;

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

        void OnMapBuilt(MapLayout layout) => currentShelterId = -1;
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

        void DrawPrompt()
        {
            var root = GameRoot.Instance;
            if (root == null || !CanDeliver(root.Run)) return;
            GUI.Label(new Rect(6, Screen.height - 30, 300, 20), "E: Entregar carga");
        }
    }
}
