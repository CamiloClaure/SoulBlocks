using System.Collections.Generic;
using UnityEngine;

namespace SoulBlocks
{
    // Q: pulls nearby souls toward the player for a short window, nearest-first, only as many
    // as fit the free cargo. The expedition crew's pickup collects them on contact; this ability
    // only moves them (ISoul.MoveTo), it never calls Collect.
    public sealed class MagnetAbility : MonoBehaviour
    {
        float cooldownRemaining;
        float activeRemaining;
        readonly List<ISoul> pulling = new List<ISoul>();

        public bool Active => activeRemaining > 0f;
        public float CooldownRemaining => Mathf.Max(0f, cooldownRemaining);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() => GameRoot.Register<MagnetAbility>();

        void Update()
        {
            var root = GameRoot.Instance;
            if (root == null || root.Player == null) return;

            if (cooldownRemaining > 0f) cooldownRemaining -= Time.deltaTime;

            if (activeRemaining > 0f)
            {
                activeRemaining -= Time.deltaTime;
                Pull(root);
                if (activeRemaining <= 0f) pulling.Clear();
            }

            if (Keys.MagnetPressed) TryMagnet();
        }

        public bool TryMagnet()
        {
            var root = GameRoot.Instance;
            if (root == null || root.Player == null || cooldownRemaining > 0f || Active) return false;

            int freeCapacity = Mathf.RoundToInt(root.Stats.Get(StatId.CargoCapacity)) - root.Run.cargoSouls;
            if (freeCapacity <= 0)
            {
                Hud.Toast("Carga llena: el iman no atrae");
                return false;
            }

            var souls = new List<(Vector3 pos, int value)>();
            var available = new List<ISoul>();
            foreach (var soul in SoulRegistry.All)
            {
                if (!soul.Available) continue;
                souls.Add((soul.Position, soul.Value));
                available.Add(soul);
            }

            var indices = MagnetSelection.Select(root.Player.transform.position, root.Stats.Get(StatId.MagnetRadius), freeCapacity, souls);
            pulling.Clear();
            foreach (int index in indices) pulling.Add(available[index]);

            cooldownRemaining = root.Stats.Get(StatId.MagnetCooldown);
            activeRemaining = root.Stats.Get(StatId.MagnetDuration);
            GameEvents.RaiseMagnetUsed();
            return true;
        }

        void Pull(GameRoot root)
        {
            Vector3 target = root.Player.transform.position;
            float step = Balance.MagnetPullSpeed * Time.deltaTime;
            for (int i = pulling.Count - 1; i >= 0; i--)
            {
                var soul = pulling[i];
                if (!soul.Available) { pulling.RemoveAt(i); continue; }
                soul.MoveTo(Vector3.MoveTowards(soul.Position, target, step));
            }
        }
    }
}
