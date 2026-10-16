using System.Collections.Generic;
using UnityEngine;

namespace SoulBlocks
{
    // Fixed layout for tests that need a map without the scene: three straight branches out of
    // a hub at the origin (Prisons +Z, Souls +X, Exploration -X), four 30 m tiles each, every
    // prison tier, a Raiz3 deep stretch with the Nucleo behind Despertar, a Raiz2 connection,
    // shelters, a risk altar and one portal per branch.
    public static class MapLayoutFixtures
    {
        public static MapLayout Simple()
        {
            var layout = new MapLayout { hubCenter = Vector3.zero };
            var prisons = Branch(layout, BranchSpecialty.Prisons, Vector3.forward, 4);
            var souls = Branch(layout, BranchSpecialty.Souls, Vector3.right, 4);
            var exploration = Branch(layout, BranchSpecialty.Exploration, Vector3.left, 4);
            Tiles(layout, prisons.id, Vector3.forward, 4, 2, UnlockIds.Raiz3);

            Prison(layout, prisons, 1, PrisonTier.T1);
            Prison(layout, prisons, 2, PrisonTier.T2, PrisonProp.Resonant, 0);
            Prison(layout, prisons, 2.2f, PrisonTier.T2, PrisonProp.Resonant, 0);
            Prison(layout, prisons, 3, PrisonTier.T3, PrisonProp.ExposedCore);
            prisons.endPrisonId = Prison(layout, prisons, 3.8f, PrisonTier.T3, branchEnd: true).id;
            Prison(layout, prisons, 4.5f, PrisonTier.T4, unlock: UnlockIds.Raiz3);
            Prison(layout, prisons, 5.8f, PrisonTier.Nucleo, unlock: UnlockIds.Despertar);

            Prison(layout, souls, 1, PrisonTier.T1);
            Prison(layout, souls, 2, PrisonTier.T1);
            souls.endPrisonId = Prison(layout, souls, 3.8f, PrisonTier.T2, branchEnd: true).id;

            Prison(layout, exploration, 1.5f, PrisonTier.T1);
            exploration.endPrisonId = Prison(layout, exploration, 3.8f, PrisonTier.T2, branchEnd: true).id;

            layout.shelters.Add(new ShelterLayout { id = 0, position = At(prisons, 4f), radius = 12f, branchId = prisons.id });
            layout.shelters.Add(new ShelterLayout { id = 1, position = At(exploration, 2f), radius = 5f, branchId = exploration.id });
            layout.altars.Add(new AltarLayout { id = 0, position = At(exploration, 3f), branchId = exploration.id });
            foreach (var branch in new[] { prisons, souls, exploration })
                layout.portals.Add(new PortalLayout { id = layout.portals.Count, position = At(branch, 3.95f), branchId = branch.id });

            // Bridge from 30 m up the Prisons branch to 30 m out on the Souls one.
            var bridge = new ConnectionLayout { id = 0, fromBranch = prisons.id, toBranch = souls.id, requiresUnlock = UnlockIds.Raiz2 };
            bridge.tiles.Add(AddTile(layout, -1, new Vector3(15f, 0f, 30f), 0f, UnlockIds.Raiz2).index);
            bridge.tiles.Add(AddTile(layout, -1, new Vector3(30f, 0f, 15f), 0f, UnlockIds.Raiz2).index);
            layout.connections.Add(bridge);
            return layout;
        }

        static BranchLayout Branch(MapLayout layout, BranchSpecialty specialty, Vector3 direction, int tiles)
        {
            var branch = new BranchLayout { id = layout.branches.Count, specialty = specialty, anchor = direction * 10f, direction = direction, length = tiles * Balance.TileSize };
            layout.branches.Add(branch);
            Tiles(layout, branch.id, direction, 0, tiles, "");
            return branch;
        }

        static List<TileLayout> Tiles(MapLayout layout, int branchId, Vector3 direction, int first, int count, string unlock)
        {
            var added = new List<TileLayout>();
            for (int k = first; k < first + count; k++)
                added.Add(AddTile(layout, branchId, direction * (10f + (k + .5f) * Balance.TileSize), (k + .5f) * Balance.TileSize, unlock));
            return added;
        }

        static TileLayout AddTile(MapLayout layout, int branchId, Vector3 position, float pathDistance, string unlock)
        {
            var tile = new TileLayout { index = layout.tiles.Count, position = position, branchId = branchId, pathDistance = pathDistance, requiresUnlock = unlock };
            layout.tiles.Add(tile);
            return tile;
        }

        // `tiles` tile lengths out along the branch.
        static Vector3 At(BranchLayout branch, float tiles) => branch.anchor + branch.direction * (tiles * Balance.TileSize);

        static PrisonLayout Prison(MapLayout layout, BranchLayout branch, float tiles, PrisonTier tier, PrisonProp props = PrisonProp.None, int group = -1, bool branchEnd = false, string unlock = "")
        {
            var prison = new PrisonLayout { id = layout.prisons.Count, position = At(branch, tiles), tier = tier, props = props, groupId = group, branchId = branch.id, branchEnd = branchEnd, requiresUnlock = unlock };
            layout.prisons.Add(prison);
            return prison;
        }
    }
}
