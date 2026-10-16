using System;
using System.Collections.Generic;
using UnityEngine;

namespace SoulBlocks
{
    // All shared persisted state, JsonUtility-serializable (lists, not sets/dictionaries).
    // Partial: a feature adds its own private persisted fields in its own RunState.<Feature>.cs.
    [Serializable]
    public sealed partial class RunState
    {
        public int version = SaveStore.CurrentVersion;
        // Hidden RNG seed, random per new game: keeps a save's rolls (blessings, plans, altars)
        // reproducible. The world itself is hand-built, no seed shapes it.
        public int seed;
        public int expeditionIndex;
        public float playTimeSeconds;
        public bool inExpedition;
        public bool victory;
        public int reserveSouls;
        public int wisdom;
        public float energy;
        public int treeStage;
        public List<string> unlocks = new List<string>();
        public List<UpgradeLevel> upgrades = new List<UpgradeLevel>();
        public List<PrisonRecord> prisons = new List<PrisonRecord>();
        public int[] brokenByTier = new int[5];
        public List<int> discoveredTiles = new List<int>();
        public ExpeditionPlan plan = new ExpeditionPlan();
        public List<string> activeBlessings = new List<string>();
        public int totalSoulsCollected;
        public int totalPrisonsBroken;

        public bool HasUnlock(string id) => unlocks.Contains(id);

        public bool AddUnlock(string id)
        {
            if (HasUnlock(id)) return false;
            unlocks.Add(id);
            return true;
        }

        public int UpgradeLevel(string id)
        {
            foreach (var upgrade in upgrades)
                if (upgrade.id == id) return upgrade.level;
            return 0;
        }

        public void SetUpgradeLevel(string id, int level)
        {
            foreach (var upgrade in upgrades)
            {
                if (upgrade.id != id) continue;
                upgrade.level = level;
                return;
            }
            upgrades.Add(new UpgradeLevel { id = id, level = level });
        }

        // Get-or-create: every caller can ask for a prison record without worrying who touched it first.
        public PrisonRecord Prison(int id)
        {
            foreach (var prison in prisons)
                if (prison.id == id) return prison;
            var created = new PrisonRecord { id = id };
            prisons.Add(created);
            return created;
        }
    }

    [Serializable]
    public sealed class UpgradeLevel
    {
        public string id;
        public int level;
    }

    [Serializable]
    public sealed class PrisonRecord
    {
        public int id;
        public int damage;
        public int phasesDone;
        public bool broken;
    }


    [Serializable]
    public sealed class ExpeditionPlan
    {
        public List<ObjectiveRecord> offered = new List<ObjectiveRecord>();
        public int chosenObjective = -1;
        public AnomalyRecord anomaly;
        public int hintBranch = -1;
    }

    [Serializable]
    public sealed class ObjectiveRecord
    {
        public string kind;
        public int targetId;
        public int branchId;
        public int goal;
        public int progress;
        public bool done;
        public int rewardWisdom;
        public int rewardSouls;
    }

    [Serializable]
    public sealed class AnomalyRecord
    {
        public string kind;
        public int branchId;
        public Vector3 position;
        public bool consumed;
    }
}
