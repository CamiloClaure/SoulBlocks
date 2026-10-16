using System;
using System.Collections.Generic;
using UnityEngine;

namespace SoulBlocks
{
    // Owns the expedition loop: energy drain while off the island (paused in shelters), start
    // and end, and the HUD panel that shows them. Portals end an expedition through the public
    // API below instead of touching RunState directly. Souls are not carried: they follow the
    // torch (WanderingSpirit), and the ones left behind when it ends simply keep wandering.
    public sealed class ExpeditionController : MonoBehaviour
    {
        public static ExpeditionController Instance { get; private set; }

        readonly EnergyMeter meter = new EnergyMeter();
        readonly HashSet<int> activeShelterIds = new HashSet<int>();
        float energyChangedAccumulator;
        bool firstPlanDone;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() => GameRoot.Register<ExpeditionController>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Instance = null;

        void OnEnable()
        {
            Instance = this;
            GameEvents.MapBuilt += OnMapBuilt;
            GameEvents.EnteredHub += OnEnteredHub;
            GameEvents.LeftHub += OnLeftHub;
            GameEvents.ShelterEntered += OnShelterEntered;
            GameEvents.ShelterExited += OnShelterExited;
            Hud.Register(100, DrawHud);
        }

        void OnDisable()
        {
            if (Instance == this) Instance = null;
            GameEvents.MapBuilt -= OnMapBuilt;
            GameEvents.EnteredHub -= OnEnteredHub;
            GameEvents.LeftHub -= OnLeftHub;
            GameEvents.ShelterEntered -= OnShelterEntered;
            GameEvents.ShelterExited -= OnShelterExited;
            Hud.Unregister(DrawHud);
        }

        void OnMapBuilt(MapLayout layout)
        {
            if (firstPlanDone) return;
            firstPlanDone = true;
            GameEvents.RaiseExpeditionPlanned();
        }

        void OnShelterEntered(int id) => activeShelterIds.Add(id);
        void OnShelterExited(int id) => activeShelterIds.Remove(id);

        void OnLeftHub()
        {
            var root = GameRoot.Instance;
            if (root == null || root.Run == null || root.Run.inExpedition) return;
            StartExpedition(root);
        }

        void OnEnteredHub()
        {
            var root = GameRoot.Instance;
            if (root == null || root.Run == null || !root.Run.inExpedition) return;
            EndExpedition(ExpeditionEndReason.OnFoot);
        }

        void StartExpedition(GameRoot root)
        {
            var run = root.Run;
            run.inExpedition = true;
            run.expeditionIndex++;
            float max = root.Stats.Get(StatId.EnergyMax);
            run.energy = max;
            meter.ResetForExpedition(max);
            activeShelterIds.Clear();
            energyChangedAccumulator = 0f;
            GameEvents.RaiseExpeditionStarted(run.expeditionIndex);
            GameEvents.RaiseEnergyChanged(run.energy, max);
        }

        // Portals call this to end an expedition on arrival.
        public void EndExpedition(ExpeditionEndReason reason)
        {
            var root = GameRoot.Instance;
            if (root == null || root.Run == null || !root.Run.inExpedition) return;
            var run = root.Run;
            run.inExpedition = false;
            float max = root.Stats.Get(StatId.EnergyMax);
            run.energy = max;
            meter.ResetForExpedition(max);
            GameEvents.RaiseEnergyChanged(run.energy, max);
            GameEvents.RaiseExpeditionEnded(reason);
            GameEvents.RaiseExpeditionPlanned();
        }

        void Update()
        {
            var root = GameRoot.Instance;
            if (root == null || root.Run == null || root.Stats == null) return;
            var run = root.Run;
            if (!run.inExpedition) return;

            float max = root.Stats.Get(StatId.EnergyMax);
            meter.SyncFrom(run.energy, max);
            bool sheltered = activeShelterIds.Count > 0;
            float? warning = meter.Tick(Time.deltaTime, sheltered);
            run.energy = meter.Current;

            energyChangedAccumulator += Time.deltaTime;
            if (energyChangedAccumulator >= .1f || meter.Depleted)
            {
                energyChangedAccumulator = 0f;
                GameEvents.RaiseEnergyChanged(run.energy, max);
            }

            if (warning.HasValue)
            {
                GameEvents.RaiseEnergyWarning(warning.Value);
                Hud.Toast(warning.Value <= Balance.EnergyWarningLow + .0001f ? "Energía 10%: volvé al árbol" : "Energía 25%");
            }

            if (meter.Depleted)
            {
                TeleportNearTree(root);
                EndExpedition(ExpeditionEndReason.EnergyDepleted);
            }
        }

        static void TeleportNearTree(GameRoot root)
        {
            if (root.Player == null || root.Tree == null) return;
            // Beside the tree, never on it: its trunk has a collider.
            Vector3 center = root.Tree.position;
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI * 2f / 8f;
                Vector3 candidate = center + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * Balance.HubReturnOffset;
                if (SoulPlayer.IsSupportedAt(candidate)) { root.Player.Teleport(candidate); return; }
            }
            root.Player.Teleport(center); // ponytail: no supported point found nearby, drop them on the tree anyway
        }

        void DrawHud()
        {
            var root = GameRoot.Instance;
            if (root == null || root.Run == null) return;
            var run = root.Run;
            float max = root.Stats != null ? root.Stats.Get(StatId.EnergyMax) : Balance.EnergyMax;
            float fraction = max > 0f ? run.energy / max : 0f;
            int capacity = root.Stats != null ? root.Stats.GetInt(StatId.SpiritCapacity) : (int)Balance.SpiritCapacity;

            var previous = GUI.color;
            GUI.color = fraction <= Balance.EnergyWarningLow ? Color.red : fraction <= Balance.EnergyWarningHigh ? Color.yellow : Color.white;
            Hud.Line(string.Format("Energía {0:0}/{1:0}", run.energy, max));
            GUI.color = previous;
            Hud.Line("Almas siguiéndote " + WanderingSpirit.FollowingCount + "/" + capacity + "  ·  Reserva " + run.reserveSouls);
            Hud.Line("Wisdom " + run.wisdom + "  ·  Expedición #" + run.expeditionIndex);
        }
    }
}
