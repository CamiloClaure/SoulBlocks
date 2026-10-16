using System.Collections.Generic;
using UnityEngine;

namespace SoulBlocks
{
    // Extra-on-break rolls and blessings (plan 1.6/1.7/1.8): both react to PrisonSystem.Broken.
    // Owns the "blessing.<id>" / "reward.<id>" stat sources and Run.activeBlessings. Altars are
    // AltarSystem's own file: different trigger (proximity + E), different persisted state.
    public sealed class RewardSystem : MonoBehaviour
    {
        public static RewardSystem Instance { get; private set; }

        RunState run;
        StatSheet stats;
        readonly List<int> speedRewardSources = new List<int>();

        bool blessingOpen;
        List<string> blessingOptions = new List<string>();

        // Read-only for bots/PlayMode tests: what is currently offered (empty while no modal
        // is open). Keys.Blocked already tells callers whether some modal (ours or the shop's)
        // is open at all.
        public IReadOnlyList<string> BlessingOptions => blessingOptions;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() => GameRoot.Register<RewardSystem>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void OnEnable()
        {
            Instance = this;
            GameEvents.MapBuilt += OnMapBuilt;
            GameEvents.ExpeditionEnded += OnExpeditionEnded;
            PrisonSystem.Broken += OnPrisonBroken;
            Hud.Register(650, DrawBlessingList);
            Hud.Register(651, DrawBlessingModal);
        }

        void OnDisable()
        {
            if (Instance == this) Instance = null;
            GameEvents.MapBuilt -= OnMapBuilt;
            GameEvents.ExpeditionEnded -= OnExpeditionEnded;
            PrisonSystem.Broken -= OnPrisonBroken;
            Hud.Unregister(DrawBlessingList);
            Hud.Unregister(DrawBlessingModal);
            if (blessingOpen) CloseModal(); // ponytail: scene can reload mid-modal, static Keys.Blocked must not stick
        }

        void OnMapBuilt(MapLayout layout)
        {
            var root = GameRoot.Instance;
            run = root.Run;
            stats = root.Stats;
            speedRewardSources.Clear();
            blessingOpen = false;

            // Fresh boot / loaded save: re-apply any blessings still listed in Run.activeBlessings
            // (chosen policy, see final report: kept across a MapBuilt, cleared only by
            // ExpeditionEnded, so a mid-expedition save/load does not silently lose them).
            foreach (var id in run.activeBlessings) ApplyBlessingStat(id);
            if (run.activeBlessings.Count > 0) GameEvents.RaiseStatsChanged();
        }

        void OnExpeditionEnded(ExpeditionEndReason reason)
        {
            foreach (var id in run.activeBlessings) stats.ClearSource("blessing." + id);
            run.activeBlessings.Clear();
            foreach (var prisonId in speedRewardSources) stats.ClearSource("reward." + prisonId);
            speedRewardSources.Clear();
            GameEvents.RaiseStatsChanged();
        }

        void OnPrisonBroken(PrisonLayout layout, Vector3 position)
        {
            RollExtraReward(layout, position);
            if (layout.branchEnd) OfferBlessing(layout.id);
        }

        void RollExtraReward(PrisonLayout layout, Vector3 position)
        {
            var roll = RewardTable.Roll(run.seed, layout.id);
            switch (roll.kind)
            {
                case RewardKind.Souls:
                    GameEvents.RaiseSoulsReleased(position, roll.amount);
                    Hud.Toast("Extra: +" + roll.amount + " almas");
                    break;

                case RewardKind.Energy:
                    float max = stats.Get(StatId.EnergyMax);
                    run.energy = Mathf.Min(max, run.energy + roll.amount);
                    GameEvents.RaiseEnergyChanged(run.energy, max);
                    Hud.Toast("Extra: +" + roll.amount + " energía");
                    break;

                case RewardKind.Speed:
                    // Unique source per prison id: several prisons granting Speed in the same
                    // expedition must not clobber each other's modifier, only the last one matters
                    // anyway (SetModifier replaces, mul does not stack), but each needs its own key
                    // to be cleared independently as souls/energy don't need clearing at all.
                    stats.SetModifier("reward." + layout.id, StatId.MoveSpeed, 0f, Balance.RewardSpeedMultiplier);
                    speedRewardSources.Add(layout.id);
                    GameEvents.RaiseStatsChanged();
                    Hud.Toast("Extra: velocidad +" + Mathf.RoundToInt((Balance.RewardSpeedMultiplier - 1f) * 100f) + "% (expedición)");
                    break;
            }
        }

        void OfferBlessing(int prisonId)
        {
            var excluded = new List<string>(run.activeBlessings);
            if (run.HasUnlock(UnlockIds.Cadena)) excluded.Add("pico_cadena");

            int count = stats.GetInt(StatId.BlessingOptions);
            var offer = BlessingTable.Offer(run.seed, prisonId, count, excluded);
            if (offer.Count == 0)
            {
                Hud.Toast("Sin bendiciones nuevas disponibles");
                return;
            }

            blessingOptions = offer;
            blessingOpen = true;
            Keys.Blocked = true;
            var player = GameRoot.Instance.Player;
            if (player != null) player.MovementLocked = true;
        }

        void CloseModal()
        {
            blessingOpen = false;
            Keys.Blocked = false;
            var player = GameRoot.Instance != null ? GameRoot.Instance.Player : null;
            if (player != null) player.MovementLocked = false;
        }

        // Public for the modal's own buttons and for bots/PlayMode tests.
        public bool ChooseBlessing(string id)
        {
            if (!blessingOpen || !blessingOptions.Contains(id)) return false;
            ApplyBlessingStat(id);
            run.activeBlessings.Add(id);
            GameEvents.RaiseBlessingChosen(id);
            GameEvents.RaiseStatsChanged();
            Hud.Toast("Bendición: " + BlessingName(id));
            CloseModal();
            return true;
        }

        void ApplyBlessingStat(string id)
        {
            switch (id)
            {
                case "dash_extra":
                    stats.SetModifier("blessing.dash_extra", StatId.DashCharges, Balance.BlessingDashChargesAdd, 1f);
                    break;
                case "energia":
                    stats.SetModifier("blessing.energia", StatId.EnergyMax, Balance.BlessingEnergyMaxAdd, 1f);
                    run.energy = Mathf.Min(stats.Get(StatId.EnergyMax), run.energy + Balance.BlessingEnergyMaxAdd);
                    break;
                case "carga":
                    stats.SetModifier("blessing.carga", StatId.SpiritCapacity, Balance.BlessingSpiritCapacityAdd, 1f);
                    break;
                // "pico_cadena" has no stat of its own: CrystalMiningAttack reads
                // Run.activeBlessings.Contains("pico_cadena") directly.
            }
        }

        static string BlessingName(string id)
        {
            switch (id)
            {
                case "dash_extra": return "carga de dash extra";
                case "pico_cadena": return "Golpe en cadena";
                case "energia": return "energía máxima";
                case "carga": return "más almas siguiendo la antorcha";
                default: return id;
            }
        }

        void DrawBlessingList()
        {
            if (run == null || !run.inExpedition || run.activeBlessings.Count == 0) return;
            foreach (var id in run.activeBlessings)
                Hud.SideLine("Bendición: " + BlessingName(id));
        }

        void DrawBlessingModal()
        {
            if (!blessingOpen) return;
            var rect = new Rect(Screen.width * .5f - 220, Screen.height * .5f - 140, 440, 280);
            GUI.Window(947002, rect, DrawModalWindow, "Bendición");
        }

        void DrawModalWindow(int windowId)
        {
            GUILayout.Label("Elegí una bendición para esta expedición:");
            foreach (var id in blessingOptions)
                if (GUILayout.Button(BlessingName(id))) ChooseBlessing(id);
        }
    }
}
