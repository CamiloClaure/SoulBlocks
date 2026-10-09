using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace SoulBlocks.Tests
{
    public class BlessingTableTests
    {
        [Test]
        public void OfferReturnsTheRequestedDistinctCount()
        {
            var offerThree = BlessingTable.Offer(111, 0, 3, null);
            Assert.AreEqual(3, offerThree.Count);
            Assert.AreEqual(3, offerThree.Distinct().Count());

            var offerFour = BlessingTable.Offer(111, 0, 4, null);
            Assert.AreEqual(4, offerFour.Count);
            Assert.AreEqual(4, offerFour.Distinct().Count());
        }

        [Test]
        public void EveryOfferedIdComesFromThePool()
        {
            var offer = BlessingTable.Offer(222, 5, 4, null);
            foreach (var id in offer) CollectionAssert.Contains(BlessingTable.AllIds, id);
        }

        [Test]
        public void SameSeedAndPrisonAlwaysOffersTheSameSet()
        {
            var a = BlessingTable.Offer(333, 9, 3, null);
            var b = BlessingTable.Offer(333, 9, 3, null);
            CollectionAssert.AreEqual(a, b);
        }

        [Test]
        public void ExcludedIdsNeverAppearInTheOffer()
        {
            var excluded = new List<string> { "rayhit_cadena", "dash_extra" };
            var offer = BlessingTable.Offer(444, 2, 3, excluded);
            Assert.AreEqual(3, offer.Count);
            foreach (var id in excluded) CollectionAssert.DoesNotContain(offer, id);
        }

        [Test]
        public void RequestingMoreThanWhatIsLeftReturnsOnlyWhatIsLeft()
        {
            var excluded = new List<string> { "rayhit_cadena", "dash_extra", "iman_amplio" };
            var offer = BlessingTable.Offer(555, 1, 4, excluded);
            Assert.AreEqual(2, offer.Count); // only "energia" and "carga" remain
        }
    }
}
