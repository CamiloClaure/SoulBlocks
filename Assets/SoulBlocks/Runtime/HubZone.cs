using UnityEngine;

namespace SoulBlocks
{
    // "In hub" = there is island ground under the player. Path ribbons start a little inside
    // the island (just under its surface), so look through every hit, not only the first.
    public sealed class HubZone : MonoBehaviour
    {
        public bool InHub { get; private set; }
        readonly RaycastHit[] hits = new RaycastHit[8];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() => GameRoot.Register<HubZone>();

        void Update()
        {
            var root = GameRoot.Instance;
            if (root == null || root.Player == null || root.Island == null) return;

            bool inHub = false;
            int count = Physics.RaycastNonAlloc(root.Player.transform.position + Vector3.up, Vector3.down, hits, 3f, (1 << 8) | (1 << 9), QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count && !inHub; i++)
                inHub = hits[i].collider.transform.IsChildOf(root.Island);

            if (inHub == InHub) return;
            InHub = inHub;
            if (inHub) GameEvents.RaiseEnteredHub();
            else GameEvents.RaiseLeftHub();
        }
    }
}
