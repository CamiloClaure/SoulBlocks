using System.Collections.Generic;
using UnityEngine;

namespace SoulBlocks
{
    public enum UpgradeTree { Souls, Wisdom }
    public enum Currency { Souls, Wisdom }

    // One effect of one level: Get(stat) folds every source's (add, mul) together (StatSheet).
    public struct UpgradeEffect
    {
        public StatId stat;
        public float add;
        public float mul;
        public UpgradeEffect(StatId stat, float add, float mul = 1f) { this.stat = stat; this.add = add; this.mul = mul; }
    }

    // Static definition of one node in either tree. Data only, no behaviour: ProgressionRules
    // reads these to compute cost, gates and stat effects.
    public sealed class UpgradeDef
    {
        public string id;
        public UpgradeTree tree;
        public string name; // Spanish display name
        public string description; // Spanish description
        public int maxLevel;
        public int baseCost;
        public float growth = 1.5f;
        public Currency currency;
        public List<UpgradeEffect> perLevelEffects = new List<UpgradeEffect>(); // summed * level
        public int milestoneEvery; // 0 = no milestone
        public List<UpgradeEffect> milestoneEffects = new List<UpgradeEffect>(); // summed * (level / milestoneEvery)
        public string milestoneText = ""; // Spanish, shown as benefit preview and as the toast
        public bool core; // Wisdom nucleo nodes: must be bought in order, required for CoreComplete
        public int gateMinTierBroken = -1; // index into RunState.brokenByTier, -1 = no gate
        public int gateMinTreeStage = -1; // minimum RunState.treeStage required, -1 = no gate
        public string unlockId = ""; // RunState.unlocks id granted on purchase, "" = none
        public int setsTreeStage = -1; // RunState.treeStage raised to this on purchase, -1 = none
        // Destiny-board placement (Albion style): the parent node that must reach requiresLevel
        // first ("" = hangs off the tree at the center), and where the node sits in board cells
        // (x right, y up, center = the tree).
        public string requires = "";
        public int requiresLevel = 1;
        public Vector2 boardPos;
    }
}
