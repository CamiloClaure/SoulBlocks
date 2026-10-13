using System.Collections.Generic;
using UnityEngine;

namespace SoulBlocks
{
    // Left click next to a crystal: one pick blow on the nearest unbroken one within PickRange,
    // at most once per StatId.PickInterval; the player turns to face it. With Golpe en cadena
    // (Wisdom node or blessing), the blow that breaks a crystal jumps on to the nearest other
    // crystal within ChainRange. The crystal shows the hit itself (PrisonView: burst + punch).
    public sealed class PickAbility : MonoBehaviour
    {
        float readyAt;

        public float CooldownRemaining => Mathf.Max(0f, readyAt - Time.time);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() => GameRoot.Register<PickAbility>();

        void OnEnable() => Hud.Register(301, DrawPrompt);
        void OnDisable() => Hud.Unregister(DrawPrompt);

        void Update()
        {
            var root = GameRoot.Instance;
            if (root == null || root.Player == null || !Keys.PickPressed) return;
            var target = InReach(root.Player.transform.position);
            if (target != null) TryHit(target);
        }

        // Public for bots/PlayMode tests: one blow on target (no reach check), false while the
        // pick is still recovering from the previous one.
        public bool TryHit(IMineable target)
        {
            var root = GameRoot.Instance;
            if (root == null || root.Player == null || target == null || Time.time < readyAt) return false;
            readyAt = Time.time + Mathf.Max(.05f, root.Stats.Get(StatId.PickInterval));
            root.Player.Face(target.Position - root.Player.transform.position);

            int level = root.Stats.GetInt(StatId.PickLevel);
            target.ApplyPickHit(level);

            bool chain = root.Run.HasUnlock(UnlockIds.Cadena) || root.Run.activeBlessings.Contains("pico_cadena");
            if (chain && target.Forecast(level).Kind == ForecastKind.Broken)
            {
                var positions = new List<Vector3>(MineableRegistry.All.Count);
                foreach (var other in MineableRegistry.All) positions.Add(other.Position);
                int next = ChainSelection.Next(target.Position, Balance.ChainRange, positions, new List<int>());
                if (next >= 0) MineableRegistry.All[next].ApplyPickHit(level);
            }
            return true;
        }

        // The nearest crystal whose surface is within PickRange of the player (horizontally).
        static IMineable InReach(Vector3 player)
        {
            IMineable best = null;
            float bestDistance = float.MaxValue;
            foreach (var target in MineableRegistry.All)
            {
                Vector3 offset = target.Position - player;
                if (Mathf.Abs(offset.y) > 3f) continue;
                offset.y = 0f;
                float distance = offset.magnitude - target.Radius;
                if (distance <= Balance.PickRange && distance < bestDistance) { bestDistance = distance; best = target; }
            }
            return best;
        }

        // Hud.WorldLabel must run inside a registered draw callback, not from Update.
        void DrawPrompt()
        {
            var root = GameRoot.Instance;
            if (root == null || root.Player == null || Keys.Blocked) return;
            var target = InReach(root.Player.transform.position);
            if (target == null) return;
            Hud.WorldLabel(target.Position + Vector3.up * .4f, "Click: picar (" + target.Forecast(root.Stats.GetInt(StatId.PickLevel)).Describe() + ")");
        }
    }
}
