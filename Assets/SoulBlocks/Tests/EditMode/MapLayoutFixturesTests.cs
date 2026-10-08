using System.Linq;
using NUnit.Framework;
using SoulBlocks;

namespace SoulBlocks.Tests
{
    public class MapLayoutFixturesTests
    {
        [Test]
        public void HasThreeBranchesInSpecialtyOrder()
        {
            var layout = MapLayoutFixtures.Simple();
            Assert.AreEqual(3, layout.branches.Count);
            Assert.AreEqual(BranchSpecialty.Souls, layout.branches[0].specialty);
            Assert.AreEqual(BranchSpecialty.Prisons, layout.branches[1].specialty);
            Assert.AreEqual(BranchSpecialty.Exploration, layout.branches[2].specialty);
        }

        [Test]
        public void EveryBranchEndsInAPrison()
        {
            var layout = MapLayoutFixtures.Simple();
            foreach (var branch in layout.branches)
            {
                Assert.GreaterOrEqual(branch.endPrisonId, 0);
                Assert.IsTrue(layout.prisons.Any(p => p.id == branch.endPrisonId && p.branchEnd));
            }
        }

        [Test]
        public void IdsAreUniquePerCollection()
        {
            var layout = MapLayoutFixtures.Simple();
            Assert.AreEqual(layout.tiles.Count, layout.tiles.Select(t => t.index).Distinct().Count());
            Assert.AreEqual(layout.prisons.Count, layout.prisons.Select(p => p.id).Distinct().Count());
            Assert.AreEqual(layout.soulSpawns.Count, layout.soulSpawns.Select(s => s.id).Distinct().Count());
            Assert.AreEqual(layout.branches.Count, layout.branches.Select(b => b.id).Distinct().Count());
        }

        [Test]
        public void HasOneShelterOnTheExplorationBranch()
        {
            var layout = MapLayoutFixtures.Simple();
            Assert.AreEqual(1, layout.shelters.Count);
            Assert.AreEqual(BranchSpecialty.Exploration, layout.branches[layout.shelters[0].branchId].specialty);
        }
    }
}
