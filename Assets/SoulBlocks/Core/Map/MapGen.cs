using UnityEngine;

namespace SoulBlocks
{
    public static partial class MapGen
    {
        // ponytail: stand-in until og/mapa replaces this body with the real seeded generator
        // (turns, validator, retries, unlocks). Shape only: 3 straight branches, one per anchor,
        // specialties Souls/Prisons/Exploration in anchor order, 10 tiles each, a T1 at the end,
        // 4 soul spawns, one shelter mid the Exploration branch.
        public static MapLayout Generate(MapGenInput input)
        {
            var layout = new MapLayout
            {
                seed = input.seed,
                effectiveSeed = input.seed,
                hubCenter = input.hubCenter
            };

            int tileIndex = 0, prisonId = 0, soulId = 0, shelterId = 0;
            for (int b = 0; b < input.anchors.Count; b++)
            {
                var anchor = input.anchors[b];
                var specialty = (BranchSpecialty)(b % 3);
                var branch = new BranchLayout
                {
                    id = b,
                    specialty = specialty,
                    anchor = anchor.position,
                    direction = anchor.direction,
                    length = Balance.TileSize * 10f
                };
                layout.branches.Add(branch);

                Vector3 last = anchor.position;
                for (int t = 0; t < 10; t++)
                {
                    last = anchor.position + anchor.direction * (Balance.TileSize * (t + .5f)); // flush with the island edge
                    layout.tiles.Add(new TileLayout
                    {
                        index = tileIndex++,
                        position = last,
                        branchId = b,
                        pathDistance = Balance.TileSize * (t + .5f)
                    });
                }

                var prison = new PrisonLayout
                {
                    id = prisonId++,
                    position = last,
                    tier = PrisonTier.T1,
                    pattern = PrisonPattern.Single,
                    branchId = b,
                    branchEnd = true
                };
                layout.prisons.Add(prison);
                branch.endPrisonId = prison.id;

                for (int s = 0; s < 4; s++)
                {
                    float distance = Balance.TileSize * (1 + s * 2);
                    layout.soulSpawns.Add(new SoulSpawnLayout
                    {
                        id = soulId++,
                        position = anchor.position + anchor.direction * distance,
                        branchId = b,
                        baseCount = 5
                    });
                }

                if (specialty == BranchSpecialty.Exploration)
                {
                    layout.shelters.Add(new ShelterLayout
                    {
                        id = shelterId++,
                        position = anchor.position + anchor.direction * (branch.length * .5f),
                        radius = 3f,
                        branchId = b
                    });
                }
            }
            return layout;
        }
    }
}
