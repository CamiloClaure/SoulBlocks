using UnityEngine;

namespace SoulBlocks
{
    // Keeps SoulPlayer.SpeedMultiplier in sync with Stats so moveSpeed in the inspector stays
    // the design-time base (6) and every bonus flows through StatSheet instead.
    public sealed class PlayerStatsBinder : MonoBehaviour
    {
        float baseMoveSpeed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { if (GameRoot.ShouldBoot) GameRoot.Attach<PlayerStatsBinder>(); }

        void OnEnable()
        {
            baseMoveSpeed = Balance.MoveSpeed;
            GameEvents.StatsChanged += Apply;
            Apply();
        }

        void OnDisable() { GameEvents.StatsChanged -= Apply; }

        void Apply()
        {
            var root = GameRoot.Instance;
            if (root == null || root.Player == null || root.Stats == null || baseMoveSpeed <= 0f) return;
            root.Player.SpeedMultiplier = root.Stats.Get(StatId.MoveSpeed) / baseMoveSpeed;
        }
    }
}
