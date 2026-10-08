using System.Collections.Generic;
using UnityEngine;

namespace SoulBlocks
{
    // Fixed layout used by tests of every crew: same shape as the MapGen stand-in, fixed
    // anchors at the origin so tests never depend on island geometry.
    public static class MapLayoutFixtures
    {
        public static MapLayout Simple()
        {
            var input = new MapGenInput
            {
                seed = 12345,
                hubCenter = Vector3.zero,
                anchors = new List<MapAnchor>
                {
                    new MapAnchor { position = new Vector3(0, 0, 10), direction = Vector3.forward },
                    new MapAnchor { position = new Vector3(10, 0, 0), direction = Vector3.right },
                    new MapAnchor { position = new Vector3(-10, 0, 0), direction = Vector3.left }
                }
            };
            return MapGen.Generate(input);
        }
    }
}
