using UnityEngine;

namespace SoulBlocks
{
    // Collects any ISoul within PickupRadius (horizontally) of the player, every frame, into Run.cargoSouls.
    // Works in the hub too: a soul picked up while HubZone.InHub deposits straight to the
    // reserve instead of waiting for the next expedition to end.
    public sealed class SoulCollector : MonoBehaviour
    {
        const float VerticalReach = 2.5f;
        float cargoFullToastAt = -999f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() => GameRoot.Register<SoulCollector>();

        void Update()
        {
            var root = GameRoot.Instance;
            if (root == null || root.Player == null || root.Run == null || root.Stats == null) return;
            var run = root.Run;
            float radius = root.Stats.Get(StatId.PickupRadius);
            float radiusSqr = radius * radius;
            int capacity = root.Stats.GetInt(StatId.CargoCapacity);
            Vector3 playerPosition = root.Player.transform.position;
            bool cargoFull = false;

            // Snapshot: Collect() below can remove entries from SoulRegistry.All mid-loop.
            foreach (var soul in SoulRegistry.All.ToArray())
            {
                if (!soul.Available) continue;
                // Horizontal reach: souls float above the path, so the player picks them up by
                // walking underneath (within VerticalReach).
                Vector3 offset = soul.Position - playerPosition;
                if (Mathf.Abs(offset.y) > VerticalReach || offset.x * offset.x + offset.z * offset.z > radiusSqr) continue;
                if (run.cargoSouls >= capacity) { cargoFull = true; continue; }

                int accepted = Cargo.Add(ref run.cargoSouls, soul.Value, capacity);
                if (accepted <= 0) { cargoFull = true; continue; }

                soul.Collect();
                run.totalSoulsCollected += accepted;
                GameEvents.RaiseSoulsCollected(accepted);
                GameEvents.RaiseCargoChanged(run.cargoSouls, capacity);
            }

            var hubZone = root.GetComponent<HubZone>();
            if (hubZone != null && hubZone.InHub) ExpeditionController.Instance?.DepositCargo();

            if (cargoFull && Time.time - cargoFullToastAt > 2f)
            {
                cargoFullToastAt = Time.time;
                Hud.Toast("Carga llena");
            }
        }
    }
}
