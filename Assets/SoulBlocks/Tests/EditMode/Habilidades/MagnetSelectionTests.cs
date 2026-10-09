using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using SoulBlocks;

namespace SoulBlocks.Tests
{
    public class MagnetSelectionTests
    {
        [Test]
        public void SelectsNearestFirst()
        {
            var souls = new List<(Vector3 pos, int value)>
            {
                (new Vector3(5, 0, 0), 1),
                (new Vector3(1, 0, 0), 1),
                (new Vector3(3, 0, 0), 1)
            };
            var selected = MagnetSelection.Select(Vector3.zero, 10f, 10, souls);
            CollectionAssert.AreEqual(new[] { 1, 2, 0 }, selected);
        }

        [Test]
        public void SkipsSoulsOutsideRadius()
        {
            var souls = new List<(Vector3 pos, int value)> { (new Vector3(20, 0, 0), 1) };
            var selected = MagnetSelection.Select(Vector3.zero, 8f, 10, souls);
            Assert.AreEqual(0, selected.Count);
        }

        [Test]
        public void StopsOnceCapacityIsFull()
        {
            var souls = new List<(Vector3 pos, int value)>
            {
                (new Vector3(1, 0, 0), 5),
                (new Vector3(2, 0, 0), 5),
                (new Vector3(3, 0, 0), 5)
            };
            var selected = MagnetSelection.Select(Vector3.zero, 10f, 10, souls);
            CollectionAssert.AreEqual(new[] { 0, 1 }, selected);
        }

        [Test]
        public void SkipsASoulThatWouldOverflowButTakesASmallerFartherOne()
        {
            var souls = new List<(Vector3 pos, int value)>
            {
                (new Vector3(1, 0, 0), 8),
                (new Vector3(2, 0, 0), 2)
            };
            var selected = MagnetSelection.Select(Vector3.zero, 10f, 5, souls);
            CollectionAssert.AreEqual(new[] { 1 }, selected);
        }

        [Test]
        public void ZeroFreeCapacitySelectsNothing()
        {
            var souls = new List<(Vector3 pos, int value)> { (new Vector3(1, 0, 0), 1) };
            var selected = MagnetSelection.Select(Vector3.zero, 10f, 0, souls);
            Assert.AreEqual(0, selected.Count);
        }
    }
}
