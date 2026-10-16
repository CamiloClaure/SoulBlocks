using System;
using UnityEngine;

namespace SoulBlocks
{
    // Every cross-system signal. Systems subscribe in their own OnEnable/OnDisable; nobody
    // maintains a central list of listeners.
    public static partial class GameEvents
    {
        public static event Action<MapLayout> MapBuilt;
        public static event Action EnteredHub;
        public static event Action LeftHub;
        public static event Action<int> ExpeditionStarted;
        public static event Action<ExpeditionEndReason> ExpeditionEnded;
        public static event Action ExpeditionPlanned;
        public static event Action<float, float> EnergyChanged;
        public static event Action<float> EnergyWarning;
        public static event Action<int> SoulsCollected;
        public static event Action<int> SoulsDeposited;
        public static event Action<Vector3, int> SoulsReleased; // loose souls to spawn on the ground (prisons, altars)
        public static event Action<int> WisdomChanged;
        public static event Action<int, int, int> PrisonDamaged;
        public static event Action<int, int> PrisonPhaseCompleted;
        public static event Action<int, PrisonTier> PrisonBroken;
        public static event Action<string> UnlockGranted;
        public static event Action<string, int> UpgradePurchased;
        public static event Action StatsChanged;
        public static event Action<int> TreeStageChanged;
        public static event Action<int> ShelterEntered;
        public static event Action<int> ShelterExited;
        public static event Action<int> TileDiscovered;
        public static event Action DashUsed;
        public static event Action<int> ObjectiveCompleted;
        public static event Action<string> BlessingChosen;
        public static event Action GameWon;

        public static void RaiseMapBuilt(MapLayout layout) => MapBuilt?.Invoke(layout);
        public static void RaiseEnteredHub() => EnteredHub?.Invoke();
        public static void RaiseLeftHub() => LeftHub?.Invoke();
        public static void RaiseExpeditionStarted(int index) => ExpeditionStarted?.Invoke(index);
        public static void RaiseExpeditionEnded(ExpeditionEndReason reason) => ExpeditionEnded?.Invoke(reason);
        public static void RaiseExpeditionPlanned() => ExpeditionPlanned?.Invoke();
        public static void RaiseEnergyChanged(float current, float max) => EnergyChanged?.Invoke(current, max);
        public static void RaiseEnergyWarning(float fraction) => EnergyWarning?.Invoke(fraction);
        public static void RaiseSoulsCollected(int amount) => SoulsCollected?.Invoke(amount);
        public static void RaiseSoulsDeposited(int amount) => SoulsDeposited?.Invoke(amount);
        public static void RaiseSoulsReleased(Vector3 position, int count) => SoulsReleased?.Invoke(position, count);
        public static void RaiseWisdomChanged(int total) => WisdomChanged?.Invoke(total);
        public static void RaisePrisonDamaged(int prisonId, int damage, int remainingHp) => PrisonDamaged?.Invoke(prisonId, damage, remainingHp);
        public static void RaisePrisonPhaseCompleted(int prisonId, int phase) => PrisonPhaseCompleted?.Invoke(prisonId, phase);
        public static void RaisePrisonBroken(int prisonId, PrisonTier tier) => PrisonBroken?.Invoke(prisonId, tier);
        public static void RaiseUnlockGranted(string id) => UnlockGranted?.Invoke(id);
        public static void RaiseUpgradePurchased(string id, int level) => UpgradePurchased?.Invoke(id, level);
        public static void RaiseStatsChanged() => StatsChanged?.Invoke();
        public static void RaiseTreeStageChanged(int stage) => TreeStageChanged?.Invoke(stage);
        public static void RaiseShelterEntered(int id) => ShelterEntered?.Invoke(id);
        public static void RaiseShelterExited(int id) => ShelterExited?.Invoke(id);
        public static void RaiseTileDiscovered(int index) => TileDiscovered?.Invoke(index);
        public static void RaiseDashUsed() => DashUsed?.Invoke();
        public static void RaiseObjectiveCompleted(int index) => ObjectiveCompleted?.Invoke(index);
        public static void RaiseBlessingChosen(string id) => BlessingChosen?.Invoke(id);
        public static void RaiseGameWon() => GameWon?.Invoke();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnLoad() => ResetAll();

        public static void ResetAll()
        {
            MapBuilt = null;
            EnteredHub = null;
            LeftHub = null;
            ExpeditionStarted = null;
            ExpeditionEnded = null;
            ExpeditionPlanned = null;
            EnergyChanged = null;
            EnergyWarning = null;
            SoulsCollected = null;
            SoulsDeposited = null;
            SoulsReleased = null;
            WisdomChanged = null;
            PrisonDamaged = null;
            PrisonPhaseCompleted = null;
            PrisonBroken = null;
            UnlockGranted = null;
            UpgradePurchased = null;
            StatsChanged = null;
            TreeStageChanged = null;
            ShelterEntered = null;
            ShelterExited = null;
            TileDiscovered = null;
            DashUsed = null;
            ObjectiveCompleted = null;
            BlessingChosen = null;
            GameWon = null;
        }
    }
}
