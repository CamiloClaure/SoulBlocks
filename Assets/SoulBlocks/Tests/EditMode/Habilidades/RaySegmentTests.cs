using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using SoulBlocks;

namespace SoulBlocks.Tests
{
    public class RaySegmentTests
    {
        [Test]
        public void OrdersHitsByDistanceAlongTheSegment()
        {
            var targets = new List<(Vector3 pos, float radius)>
            {
                (new Vector3(8, 0, 0), .5f),
                (new Vector3(2, 0, 0), .5f),
                (new Vector3(5, 0, 0), .5f)
            };
            var hits = RaySegment.Hits(Vector3.zero, new Vector3(10, 0, 0), targets);
            CollectionAssert.AreEqual(new[] { 1, 2, 0 }, hits);
        }

        [Test]
        public void TargetWithinRadiusButOffAxisHits()
        {
            var targets = new List<(Vector3 pos, float radius)> { (new Vector3(5, 0, 1f), 1.5f) };
            var hits = RaySegment.Hits(Vector3.zero, new Vector3(10, 0, 0), targets);
            Assert.AreEqual(1, hits.Count);
        }

        [Test]
        public void TargetBeyondRadiusMisses()
        {
            var targets = new List<(Vector3 pos, float radius)> { (new Vector3(5, 0, 3f), 1f) };
            var hits = RaySegment.Hits(Vector3.zero, new Vector3(10, 0, 0), targets);
            Assert.AreEqual(0, hits.Count);
        }

        [Test]
        public void TargetBeyondSegmentEndMisses()
        {
            var targets = new List<(Vector3 pos, float radius)> { (new Vector3(20, 0, 0), 1f) };
            var hits = RaySegment.Hits(Vector3.zero, new Vector3(10, 0, 0), targets);
            Assert.AreEqual(0, hits.Count);
        }

        [Test]
        public void IgnoresHeightDifference()
        {
            var targets = new List<(Vector3 pos, float radius)> { (new Vector3(5, 50, 0), .5f) };
            var hits = RaySegment.Hits(Vector3.zero, new Vector3(10, 0, 0), targets);
            Assert.AreEqual(1, hits.Count);
        }
    }
}
