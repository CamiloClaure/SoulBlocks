using NUnit.Framework;
using SoulBlocks;

namespace SoulBlocks.Tests
{
    public class SeededRngTests
    {
        [Test]
        public void SameSeedSameSequence()
        {
            var a = new SeededRng(1234);
            var b = new SeededRng(1234);
            for (int i = 0; i < 50; i++)
                Assert.AreEqual(a.Next(0, 1000000), b.Next(0, 1000000));
        }

        [Test]
        public void NamedStreamsDiffer()
        {
            var a = SeededRng.Stream(42, "mapa");
            var b = SeededRng.Stream(42, "prisiones");
            bool anyDifferent = false;
            for (int i = 0; i < 20; i++)
                if (a.Next(0, 1000000) != b.Next(0, 1000000)) anyDifferent = true;
            Assert.IsTrue(anyDifferent);
        }

        [Test]
        public void StreamUnaffectedByConsumingAnotherStream()
        {
            var untouched = SeededRng.Stream(7, "habilidades");
            var expected = new int[10];
            for (int i = 0; i < expected.Length; i++) expected[i] = untouched.Next(0, 999);

            var reference = SeededRng.Stream(7, "habilidades");
            var other = SeededRng.Stream(7, "progresion");
            for (int i = 0; i < 1000; i++) other.Next(0, 999); // consume unrelated stream heavily

            for (int i = 0; i < expected.Length; i++)
                Assert.AreEqual(expected[i], reference.Next(0, 999));
        }

        [Test]
        public void NextFloatIsWithinZeroToOne()
        {
            var rng = new SeededRng(99);
            for (int i = 0; i < 200; i++)
            {
                float value = rng.NextFloat();
                Assert.GreaterOrEqual(value, 0f);
                Assert.Less(value, 1f);
            }
        }

        [Test]
        public void ShuffleKeepsAllElements()
        {
            var rng = new SeededRng(5);
            var list = new System.Collections.Generic.List<int> { 1, 2, 3, 4, 5, 6, 7 };
            rng.Shuffle(list);
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4, 5, 6, 7 }, list);
        }
    }
}
