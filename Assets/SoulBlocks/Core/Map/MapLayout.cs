using System;
using System.Collections.Generic;
using UnityEngine;

namespace SoulBlocks
{
    // What a branch is mostly for: souls, prisons, or exploring (shelters, altars, platforms).
    public enum BranchSpecialty { Souls, Prisons, Exploration }
    // Prison tiers, T1 to T4 plus the final Nucleo; the pickaxe color stage gates each one.
    public enum PrisonTier { T1, T2, T3, T4, Nucleo }

    // Resonant prisons share damage within their group; ExposedCore doubles every hit after the first.
    [Flags]
    public enum PrisonProp { None = 0, Resonant = 1, ExposedCore = 2 }

    // How a prison group is laid out.
    public enum PrisonPattern { Single, Line, Arc, Compact, Connected }

    // The expedition map every system reads: branches cut into tiles, prisons, shelters,
    // connections, risk altars and portals. Built from the hand-made scene by SceneLayout
    // (Runtime), or by hand in MapLayoutFixtures for tests.
    [Serializable]
    public sealed class MapLayout
    {
        public Vector3 hubCenter;
        public List<BranchLayout> branches = new List<BranchLayout>();
        public List<TileLayout> tiles = new List<TileLayout>();
        public List<PrisonLayout> prisons = new List<PrisonLayout>();
        public List<ShelterLayout> shelters = new List<ShelterLayout>();
        public List<ConnectionLayout> connections = new List<ConnectionLayout>();
        public List<AltarLayout> altars = new List<AltarLayout>();
        public List<PortalLayout> portals = new List<PortalLayout>();

        // Walking distance of the branch tile nearest to `position` (XZ): prisons and shelters
        // sit anywhere across the ribbon, not on tile centers.
        public float PathDistanceAt(int branchId, Vector3 position)
        {
            float best = float.MaxValue, distance = 0f;
            foreach (var tile in tiles)
            {
                if (tile.branchId != branchId) continue;
                float dx = tile.position.x - position.x, dz = tile.position.z - position.z;
                float d = dx * dx + dz * dz;
                if (d < best)
                { best = d; distance = tile.pathDistance; }
            }
            return distance;
        }
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
        // This tile's stretch of the path ribbon, left/right top-surface edges.
        public List<Vector3> edgeLeft = new List<Vector3>();
        public List<Vector3> edgeRight = new List<Vector3>();
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


}
