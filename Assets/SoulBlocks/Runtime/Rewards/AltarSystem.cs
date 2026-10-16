using System.Collections.Generic;
using UnityEngine;

namespace SoulBlocks
{
    // Risk altars (plan 1.8 "Altares de riesgo"): a placeholder pillar per Layout.altars entry.
    // Cost/reward come from AltarRules (fixed per altar, known before use, no hidden randomness).
    // Spent-this-expedition ids live in Run.spentAltars, refreshed on ExpeditionPlanned.
    public sealed class AltarSystem : MonoBehaviour
    {
        public static AltarSystem Instance { get; private set; }

        sealed class AltarEntry
        {
            public AltarLayout layout;
            public AltarRoll roll;
        }

        RunState run;
        StatSheet stats;
        Transform root;
        readonly Dictionary<int, AltarEntry> altars = new Dictionary<int, AltarEntry>();
        int nearAltarId = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() => GameRoot.Register<AltarSystem>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void OnEnable()
        {
            Instance = this;
            GameEvents.MapBuilt += OnMapBuilt;
            GameEvents.ExpeditionPlanned += OnExpeditionPlanned;
            Hud.Register(652, DrawLabel);
        }

        void OnDisable()
        {
            if (Instance == this) Instance = null;
            GameEvents.MapBuilt -= OnMapBuilt;
            GameEvents.ExpeditionPlanned -= OnExpeditionPlanned;
            Hud.Unregister(DrawLabel);
        }

        void OnExpeditionPlanned()
        {
            if (run != null) run.spentAltars.Clear();
        }

        void OnMapBuilt(MapLayout layout)
        {
            var gameRoot = GameRoot.Instance;
            run = gameRoot.Run;
            stats = gameRoot.Stats;
            if (root != null) Destroy(root.gameObject);
            altars.Clear();
            nearAltarId = -1;

            root = new GameObject("Generated Altars").transform;
            root.SetParent(transform, false);

            foreach (var altar in layout.altars)
            {
                altars[altar.id] = new AltarEntry { layout = altar, roll = AltarRules.Roll(run.seed, altar.id) };
                Build(altar);
            }
        }

        void Build(AltarLayout altar)
        {
            var pillar = Placeholder.Make(PrimitiveType.Cylinder, new Color(.55f, .1f, .75f), "Altar " + altar.id,
                root, altar.position + Vector3.up * .6f, new Vector3(.6f, .6f, .6f), false, .5f);
        }

        void Update()
        {
            var gameRoot = GameRoot.Instance;
            if (gameRoot == null || gameRoot.Player == null || run == null) { nearAltarId = -1; return; }

            nearAltarId = -1;
            Vector3 p = gameRoot.Player.transform.position;
            foreach (var entry in altars.Values)
            {
                if (run.spentAltars.Contains(entry.layout.id)) continue;
                if (Vector3.Distance(p, entry.layout.position) <= Balance.AltarInteractRange) { nearAltarId = entry.layout.id; break; }
            }

            if (nearAltarId >= 0 && Keys.InteractPressed) UseAltar(nearAltarId);
        }

        void DrawLabel()
        {
            if (nearAltarId < 0 || !altars.TryGetValue(nearAltarId, out var entry)) return;
            Hud.WorldLabel(entry.layout.position + Vector3.up * 1.5f,
                "E: Altar (-" + entry.roll.cost + " energía, +" + entry.roll.reward + " almas)");
        }

        // Public for the world prompt's E and for bots/PlayMode tests.
        public bool UseAltar(int altarId)
        {
            if (run == null || !altars.TryGetValue(altarId, out var entry)) return false;
            if (run.spentAltars.Contains(altarId)) return false;

            if (run.energy < entry.roll.cost)
            {
                Hud.Toast("No hay suficiente energía para el altar");
                return false;
            }

            run.energy -= entry.roll.cost;
            GameEvents.RaiseEnergyChanged(run.energy, stats.Get(StatId.EnergyMax));
            GameEvents.RaiseSoulsReleased(entry.layout.position, entry.roll.reward);
            run.spentAltars.Add(altarId);
            Hud.Toast("Altar: -" + entry.roll.cost + " energía, +" + entry.roll.reward + " almas");
            return true;
        }
    }
}
