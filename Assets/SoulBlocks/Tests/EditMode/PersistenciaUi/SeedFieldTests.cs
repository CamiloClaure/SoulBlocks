using NUnit.Framework;

namespace SoulBlocks.Tests
{
    public class SeedFieldTests
    {
        [TestCase("123", "123")]
        [TestCase("-123", "-123")]
        [TestCase("", "")]
        [TestCase(null, "")]
        [TestCase("12a3b4", "1234")]
        [TestCase("1-2-3", "123")] // minus only kept at raw index 0
        [TestCase("abc", "")]
        public void FilterKeepsDigitsAndLeadingMinusOnly(string raw, string expected)
        {
            Assert.AreEqual(expected, SeedField.Filter(raw));
        }

        [Test]
        public void TryParseValidInt()
        {
            Assert.IsTrue(SeedField.TryParse("123", out int seed));
            Assert.AreEqual(123, seed);
        }

        [Test]
        public void TryParseNegativeInt()
        {
            Assert.IsTrue(SeedField.TryParse("-456", out int seed));
            Assert.AreEqual(-456, seed);
        }

        [TestCase("")]
        [TestCase("abc")]
        [TestCase("12-3")]
        [TestCase("99999999999999999999")] // overflow
        public void TryParseGarbageFails(string text)
        {
            Assert.IsFalse(SeedField.TryParse(text, out _));
        }
    }
}
