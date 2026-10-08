using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using SoulBlocks;

namespace SoulBlocks.Tests.Mapa
{
    // The real seeded generator's own test suite. Synthetic anchors mirror GameRoot's real ones
    // (North +Z, East +X, West -X, all pointing away from hubCenter) without depending on island
    // geometry.
    public class MapGenRealTests
    {
        static MapGenInput Input(int seed) => new MapGenInput
        {
            seed = seed,
            hubCenter = new Vector3(-12, 0, -12),
            anchors = new List<MapAnchor>
            {
                new MapAnchor { position = new Vector3(-5, 0, 10), direction = Vector3.forward },
                new MapAnchor { position = new Vector3(6, 0, -12), direction = Vector3.right },
                new MapAnchor { position = new Vector3(-30, 0, -30), direction = Vector3.left }
            }
        };

        static IEnumerable<int> FiveHundredSeeds()
        {
            for (int i = 0; i < 500; i++) yield return i * 97 + 1; // spread, avoid consecutive-seed correlation
        }

        [Test]
        public void FiveHundredSeedsValidateClean()
        {
            foreach (var seed in FiveHundredSeeds())
            {
                var layout = MapGen.Generate(Input(seed));
                var failures = MapValidator.Validate(layout);
                Assert.IsEmpty(failures, "seed " + seed + " (effective " + layout.effectiveSeed + "): " + string.Join("; ", failures));
            }
        }

        [Test]
        public void FiveHundredSeedsAreDeterministic()
        {
            foreach (var seed in FiveHundredSeeds())
            {
                var a = MapGen.Generate(Input(seed));
                var b = MapGen.Generate(Input(seed));
                Assert.AreEqual(JsonUtility.ToJson(a), JsonUtility.ToJson(b), "seed " + seed + " was not deterministic");
            }
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(int.MaxValue)]
        [TestCase(int.MinValue)]
        public void ExtremeSeedsValidateClean(int seed)
        {
            var layout = MapGen.Generate(Input(seed));
            var failures = MapValidator.Validate(layout);
            Assert.IsEmpty(failures, string.Join("; ", failures));
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(int.MaxValue)]
        [TestCase(int.MinValue)]
        public void ExtremeSeedsAreDeterministic(int seed)
        {
            var a = MapGen.Generate(Input(seed));
            var b = MapGen.Generate(Input(seed));
            Assert.AreEqual(JsonUtility.ToJson(a), JsonUtility.ToJson(b));
        }

        [Test]
        public void BranchLengthsStayWithinTheTable()
        {
            foreach (var seed in FiveHundredSeeds().Take(100))
            {
                var layout = MapGen.Generate(Input(seed));
                foreach (var branch in layout.branches)
                {
                    float min, max;
                    switch (branch.specialty)
                    {
                        case BranchSpecialty.Souls: min = Balance.SoulsBranchMin; max = Balance.SoulsBranchMax; break;
                        case BranchSpecialty.Prisons: min = Balance.PrisonsBranchMin; max = Balance.PrisonsBranchMax; break;
                        default: min = Balance.ExplorationBranchMin; max = Balance.ExplorationBranchMax; break;
                    }
                    Assert.GreaterOrEqual(branch.length, min - Balance.TileSize - .01f, "seed " + seed + " branch " + branch.id);
                    Assert.LessOrEqual(branch.length, max + .01f, "seed " + seed + " branch " + branch.id);
                }
            }
        }

        [Test]
        public void FirstThreeTilesPerBranchAreStraightAndFlush()
        {
            foreach (var seed in FiveHundredSeeds().Take(100))
            {
                var layout = MapGen.Generate(Input(seed));
                foreach (var branch in layout.branches)
                {
                    var tiles = layout.tiles.Where(t => t.branchId == branch.id).OrderBy(t => t.pathDistance).Take(3).ToList();
                    Assert.AreEqual(3, tiles.Count, "seed " + seed + " branch " + branch.id + " too short");

                    Vector3 expectedFirst = branch.anchor + branch.direction * (Balance.TileSize * .5f);
                    Assert.Less((tiles[0].position - expectedFirst).magnitude, .01f, "seed " + seed + " branch " + branch.id + " not flush");

                    for (int i = 1; i < 3; i++)
                    {
                        Vector3 step = tiles[i].position - tiles[i - 1].position;
                        Vector3 expectedStep = branch.direction * Balance.TileSize;
                        Assert.Less((step - expectedStep).magnitude, .01f, "seed " + seed + " branch " + branch.id + " tile " + i + " not straight");
                    }
                }
            }
        }

        [Test]
        public void RequiredPathIsContinuousForEveryBranch()
        {
            foreach (var seed in FiveHundredSeeds().Take(100))
            {
                var layout = MapGen.Generate(Input(seed));
                foreach (var branch in layout.branches)
                {
                    var distances = layout.tiles.Where(t => t.branchId == branch.id).Select(t => t.pathDistance).Distinct().OrderBy(d => d).ToList();
                    for (int i = 1; i < distances.Count; i++)
                        Assert.Less(Mathf.Abs(distances[i] - distances[i - 1] - Balance.TileSize), .01f,
                            "seed " + seed + " branch " + branch.id + " breaks continuity at " + distances[i - 1]);
                }
            }
        }

        [Test]
        public void NucleoExistsExactlyOnceAcrossManySeeds()
        {
            foreach (var seed in FiveHundredSeeds().Take(100))
            {
                var layout = MapGen.Generate(Input(seed));
                int count = layout.prisons.Count(p => p.tier == PrisonTier.Nucleo);
                Assert.AreEqual(1, count, "seed " + seed);
                Assert.IsTrue(layout.prisons.Single(p => p.tier == PrisonTier.Nucleo).requiresUnlock == UnlockIds.Despertar);
            }
        }

        [Test]
        public void AtLeastOneConnectionExists()
        {
            var layout = MapGen.Generate(Input(42));
            Assert.GreaterOrEqual(layout.connections.Count, 1);
        }
    }
}
