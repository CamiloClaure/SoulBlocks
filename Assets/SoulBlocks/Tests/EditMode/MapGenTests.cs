using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using SoulBlocks;

namespace SoulBlocks.Tests
{
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
            Assert.AreEqual(a.tiles.Count, b.tiles.Count);
            for (int i = 0; i < a.tiles.Count; i++)
                Assert.AreEqual(a.tiles[i].position, b.tiles[i].position);
            for (int i = 0; i < a.prisons.Count; i++)
                Assert.AreEqual(a.prisons[i].position, b.prisons[i].position);
        }

        [Test]
        public void TenTilesPerBranch()
        {
            var layout = MapGen.Generate(Input(1));
            foreach (var branch in layout.branches)
            {
                int count = 0;
                foreach (var tile in layout.tiles)
                    if (tile.branchId == branch.id) count++;
                Assert.AreEqual(10, count);
            }
        }

        [Test]
        public void FourSoulSpawnsPerBranch()
        {
            var layout = MapGen.Generate(Input(2));
            foreach (var branch in layout.branches)
            {
                int count = 0;
                foreach (var spawn in layout.soulSpawns)
                    if (spawn.branchId == branch.id) count++;
                Assert.AreEqual(4, count);
            }
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
