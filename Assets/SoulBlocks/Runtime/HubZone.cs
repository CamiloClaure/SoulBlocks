using UnityEngine;

namespace SoulBlocks
{
    // "In hub" = the ground under the player belongs to the island, not a generated tile.
    public sealed class HubZone : MonoBehaviour
    {
        public bool InHub { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() => GameRoot.Register<HubZone>();

        void Update()
        {
            var root = GameRoot.Instance;
            if (root == null || root.Player == null || root.Island == null) return;

            bool inHub = false;
            if (Physics.Raycast(root.Player.transform.position + Vector3.up, Vector3.down, out var hit, 3f, (1 << 8) | (1 << 9), QueryTriggerInteraction.Ignore))
                inHub = hit.collider.transform.IsChildOf(root.Island);

            if (inHub == InHub) return;
            InHub = inHub;
            if (inHub) GameEvents.RaiseEnteredHub();
            else GameEvents.RaiseLeftHub();
        }
    }
}
