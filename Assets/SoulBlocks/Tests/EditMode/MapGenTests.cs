using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using SoulBlocks;

namespace SoulBlocks.Tests
{
    // ponytail: kept lean. The exhaustive 500-seed/extreme-seed/shape suite that replaces the old
    // stand-in assertions (10 tiles/branch, 4 soul spawns/branch) lives in Tests/EditMode/Mapa/.
    public class MapGenTests
    {
        static MapGenInput Input(int seed) => new MapGenInput
        {
            seed = seed,
            hubCenter = Vector3.zero,
            anchors = new List<MapAnchor>
            {
                new MapAnchor { position = new Vector3(0, 0, 10), direction = Vector3.forward },
                new MapAnchor { position = new Vector3(10, 0, 0), direction = Vector3.right },
                new MapAnchor { position = new Vector3(-10, 0, 0), direction = Vector3.left }
            }
        };

        [Test]
        public void GenerateIsDeterministicForTheSameInput()
        {
            var a = MapGen.Generate(Input(7));
            var b = MapGen.Generate(Input(7));
            Assert.AreEqual(JsonUtility.ToJson(a), JsonUtility.ToJson(b));
        }

        [Test]
        public void EmptyAnchorsProduceEmptyLayout()
        {
            var layout = MapGen.Generate(new MapGenInput { seed = 1 });
            Assert.AreEqual(0, layout.branches.Count);
            Assert.AreEqual(0, layout.tiles.Count);
        }
    }
}
