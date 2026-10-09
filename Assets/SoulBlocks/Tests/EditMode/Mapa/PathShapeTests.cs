using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SoulBlocks.Tests.Mapa
{
    public class PathShapeTests
    {
        // A grid-style 90 degree turn: pseudo, A, B (the corner), C, pseudo.
        static List<TileLayout> Corner(float halfWidth)
        {
            var tiles = new List<TileLayout> { new TileLayout { index = 0 }, new TileLayout { index = 1 }, new TileLayout { index = 2 } };
            var points = new List<Vector3> { new Vector3(-10, 0, 0), new Vector3(0, 0, 0), new Vector3(10, 0, 0), new Vector3(10, 0, 10), new Vector3(10, 0, 20) };
            for (int i = 0; i < 3; i++) tiles[i].position = points[i + 1];
            var widths = new List<float> { halfWidth, halfWidth, halfWidth, halfWidth, halfWidth };
            PathShape.BuildPieces(points, widths, new List<TileLayout> { null, tiles[0], tiles[1], tiles[2], null });
            return tiles;
        }

        [Test]
        public void ACornerIsSmoothSharedAndNeverFolds()
        {
            var tiles = Corner(6f);
            foreach (var tile in tiles)
            {
                Assert.GreaterOrEqual(tile.edgeLeft.Count, 2);
                Assert.IsFalse(MapValidator.Folds(tile), "tile " + tile.index + " folds");
            }
            for (int i = 0; i + 1 < tiles.Count; i++)
            {
                Assert.Less((tiles[i].edgeLeft[tiles[i].edgeLeft.Count - 1] - tiles[i + 1].edgeLeft[0]).sqrMagnitude, 1e-8f, "left seam " + i);
                Assert.Less((tiles[i].edgeRight[tiles[i].edgeRight.Count - 1] - tiles[i + 1].edgeRight[0]).sqrMagnitude, 1e-8f, "right seam " + i);
            }
            // The corner's control point lies outside the curve, on the uncapped outer side.
            PathShape.CenterSpan(tiles[1], out var left, out var right);
            Assert.Less(Vector3.Distance(left, right), 12f, "the inner side of the bend should be narrowed");
        }

        [Test]
        public void RandomPointsStayOnTheRibbon()
        {
            var tiles = Corner(5f);
            var rng = new SeededRng(7);
            foreach (var tile in tiles)
                for (int n = 0; n < 200; n++)
                {
                    Vector3 p = PathShape.RandomPoint(tile, rng, .5f);
                    float best = float.MaxValue;
                    for (int k = 0; k < tile.edgeLeft.Count; k++) best = Mathf.Min(best, DistanceToSegment(p, tile.edgeLeft[k], tile.edgeRight[k]));
                    Assert.Less(best, 1f, "tile " + tile.index + " point " + p);
                }
        }

        [Test]
        public void PlatformPointsStayOnTheDisc()
        {
            var platform = new TileLayout { position = new Vector3(3, 0, 4), shortcut = true };
            var rng = new SeededRng(11);
            for (int n = 0; n < 200; n++)
                Assert.LessOrEqual(Vector3.Distance(PathShape.RandomPoint(platform, rng, .5f), platform.position), Balance.PlatformSize * .5f - .5f + 1e-4f);
        }

        internal static float DistanceToSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude));
            Vector3 d = a + ab * t - p;
            d.y = 0f;
            return d.magnitude;
        }
    }
}
