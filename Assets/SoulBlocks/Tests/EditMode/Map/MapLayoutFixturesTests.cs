using System.Linq;
using NUnit.Framework;
using SoulBlocks;

namespace SoulBlocks.Tests
{
    public class MapLayoutFixturesTests
    {
        [Test]
        public void HasThreeBranchesCoveringEachSpecialtyOnce()
        {
            var layout = MapLayoutFixtures.Simple();
            Assert.AreEqual(3, layout.branches.Count);
            Assert.AreEqual(1, layout.branches.Count(b => b.specialty == BranchSpecialty.Souls));
            Assert.AreEqual(1, layout.branches.Count(b => b.specialty == BranchSpecialty.Prisons));
            Assert.AreEqual(1, layout.branches.Count(b => b.specialty == BranchSpecialty.Exploration));
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
            Assert.AreEqual(layout.branches.Count, layout.branches.Select(b => b.id).Distinct().Count());
        }

        [Test]
        public void HasAtLeastOneShelterOnTheExplorationBranch()
        {
            var layout = MapLayoutFixtures.Simple();
            var explorationId = layout.branches.First(b => b.specialty == BranchSpecialty.Exploration).id;
            Assert.IsTrue(layout.shelters.Any(s => s.branchId == explorationId));
        }
    }
}
