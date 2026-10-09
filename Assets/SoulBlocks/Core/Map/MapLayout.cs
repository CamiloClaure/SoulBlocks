using System;
using System.Collections.Generic;
using UnityEngine;

namespace SoulBlocks
{
    public enum BranchSpecialty { Souls, Prisons, Exploration }
    public enum PrisonTier { T1, T2, T3, T4, Nucleo }

    [Flags]
    public enum PrisonProp { None = 0, Resonant = 1, ExposedCore = 2 }

    public enum PrisonPattern { Single, Line, Arc, Compact, Connected }

    [Serializable]
    public sealed class MapLayout
    {
        public int seed;
        public int effectiveSeed;
        public Vector3 hubCenter;
        public List<BranchLayout> branches = new List<BranchLayout>();
        public List<TileLayout> tiles = new List<TileLayout>();
        public List<SoulSpawnLayout> soulSpawns = new List<SoulSpawnLayout>();
        public List<PrisonLayout> prisons = new List<PrisonLayout>();
        public List<ShelterLayout> shelters = new List<ShelterLayout>();
        public List<GapLayout> gaps = new List<GapLayout>();
        public List<ConnectionLayout> connections = new List<ConnectionLayout>();
        public List<AltarLayout> altars = new List<AltarLayout>();
        public List<PortalLayout> portals = new List<PortalLayout>();
    }

    [Serializable]
    public sealed class BranchLayout
    {
        public int id;
        public BranchSpecialty specialty;
        public Vector3 anchor;
        public Vector3 direction;
        public float length;
        public int endPrisonId = -1;
    }

    [Serializable]
    public sealed class TileLayout
    {
        public int index;
        public Vector3 position; // top-surface center
        public int branchId = -1; // -1 for connections
        public float pathDistance; // walking distance from hub
        public string requiresUnlock = ""; // "" = always present
        public bool shortcut;
    }

    [Serializable]
    public sealed class SoulSpawnLayout
    {
        public int id;
        public Vector3 position;
        public int branchId;
        public int baseCount;
        public string requiresUnlock = "";
    }

    [Serializable]
    public sealed class PrisonLayout
    {
        public int id;
        public Vector3 position;
        public PrisonTier tier;
        public PrisonProp props;
        public int groupId = -1;
        public PrisonPattern pattern;
        public int branchId;
        public bool branchEnd;
        public string requiresUnlock = "";
    }

    [Serializable]
    public sealed class ShelterLayout
    {
        public int id;
        public Vector3 position;
        public float radius;
        public int branchId;
    }

    [Serializable]
    public sealed class GapLayout
    {
        public int id;
        public Vector3 from;
        public Vector3 to;
        public int branchId;
    }

    [Serializable]
    public sealed class ConnectionLayout
    {
        public int id;
        public int fromBranch;
        public int toBranch;
        public List<int> tiles = new List<int>();
        public string requiresUnlock = "";
    }

    [Serializable]
    public sealed class AltarLayout
    {
        public int id;
        public Vector3 position;
        public int branchId;
    }

    [Serializable]
    public sealed class PortalLayout
    {
        public int id;
        public Vector3 position;
        public int branchId;
    }

    [Serializable]
    public sealed class MapGenInput
    {
        public int seed;
        public List<MapAnchor> anchors = new List<MapAnchor>();
        public Vector3 hubCenter;
    }

    [Serializable]
    public sealed class MapAnchor
    {
        public Vector3 position; // island edge point, top surface
        public Vector3 direction; // unit, outward
    }
}
