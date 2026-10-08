using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using SoulBlocks;

namespace SoulBlocks.Tests
{
    public class ChainSelectionTests
    {
        [Test]
        public void PicksNearestUntouchedWithinRange()
        {
            var targets = new List<Vector3> { new Vector3(10, 0, 0), new Vector3(2, 0, 0), new Vector3(3, 0, 0) };
            int next = ChainSelection.Next(Vector3.zero, 6f, targets, new List<int>());
            Assert.AreEqual(1, next);
        }

        [Test]
        public void SkipsTouchedIndices()
        {
            var targets = new List<Vector3> { new Vector3(2, 0, 0), new Vector3(3, 0, 0) };
            int next = ChainSelection.Next(Vector3.zero, 6f, targets, new List<int> { 0 });
            Assert.AreEqual(1, next);
        }

        [Test]
        public void ReturnsMinusOneWhenNothingInRange()
        {
            var targets = new List<Vector3> { new Vector3(50, 0, 0) };
            int next = ChainSelection.Next(Vector3.zero, 6f, targets, new List<int>());
            Assert.AreEqual(-1, next);
        }

        [Test]
        public void ReturnsMinusOneWhenEverythingTouched()
        {
            var targets = new List<Vector3> { new Vector3(2, 0, 0) };
            int next = ChainSelection.Next(Vector3.zero, 6f, targets, new List<int> { 0 });
            Assert.AreEqual(-1, next);
        }
    }
}
