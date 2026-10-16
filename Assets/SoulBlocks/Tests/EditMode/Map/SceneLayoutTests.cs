using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SoulBlocks.Tests
{
    // The hand-built scene must give every expedition mechanic something to act on: three
    // branches, every prison tier, the gated deep stretch and Nucleo, a gated connection,
    // shelters, risk altars and one portal per branch, all standing on walkable ground.
    public class SceneLayoutTests
    {
        static MapLayout layout;

        [OneTimeSetUp]
        public void BuildLayout()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SoulBlocks.unity", OpenSceneMode.Single);
            foreach (var path in Object.FindObjectsByType<AuthoredPath>(FindObjectsInactive.Include, FindObjectsSortMode.None)) path.Rebuild();
            Physics.SyncTransforms();
            var tree = Object.FindAnyObjectByType<CentralSpiritTree>();
            layout = SceneLayout.FromScene(scene, tree.transform.position);
        }

        [Test]
        public void HasOneBranchPerSpecialtyEachEndingInAPrisonAndAPortal()
        {
            Assert.AreEqual(3, layout.branches.Count);
            foreach (BranchSpecialty specialty in System.Enum.GetValues(typeof(BranchSpecialty)))
                Assert.AreEqual(1, layout.branches.Count(b => b.specialty == specialty), specialty.ToString());
            foreach (var branch in layout.branches)
            {
                Assert.IsTrue(layout.tiles.Any(t => t.branchId == branch.id), "Branch " + branch.id + " has no tiles.");
                Assert.IsTrue(layout.prisons.Any(p => p.id == branch.endPrisonId && p.branchEnd), "Branch " + branch.id + " has no end prison.");
                Assert.AreEqual(1, layout.portals.Count(p => p.branchId == branch.id), "Branch " + branch.id + " portals.");
            }
        }

        [Test]
        public void HasEveryTierWithTheDeepStretchAndNucleoGated()
        {
            foreach (PrisonTier tier in System.Enum.GetValues(typeof(PrisonTier)))
                Assert.IsTrue(layout.prisons.Any(p => p.tier == tier), "No " + tier + " prison.");
            Assert.IsTrue(layout.prisons.Where(p => p.tier == PrisonTier.T4).All(p => p.requiresUnlock == UnlockIds.Raiz3));
            Assert.AreEqual(UnlockIds.Despertar, layout.prisons.Single(p => p.tier == PrisonTier.Nucleo).requiresUnlock);
            Assert.IsTrue(layout.tiles.Any(t => t.requiresUnlock == UnlockIds.Raiz3), "The deep stretch has no tiles.");
            Assert.IsTrue(layout.prisons.Any(p => p.groupId >= 0 && (p.props & PrisonProp.Resonant) != 0), "No resonant group.");
        }

        [Test]
        public void HasAGatedConnectionSheltersAndRiskAltars()
        {
            var connection = layout.connections.Single();
            Assert.AreEqual(UnlockIds.Raiz2, connection.requiresUnlock);
            Assert.Greater(connection.tiles.Count, 0);
            Assert.AreNotEqual(connection.fromBranch, connection.toBranch);
            Assert.GreaterOrEqual(layout.shelters.Count, 3);
            Assert.GreaterOrEqual(layout.altars.Count, 2);
        }

        [Test]
        public void EverySiteStandsOnGround()
        {
            var points = layout.prisons.Select(p => p.position).Concat(layout.shelters.Select(s => s.position))
                .Concat(layout.portals.Select(p => p.position)).Concat(layout.altars.Select(a => a.position));
            foreach (var point in points)
                Assert.IsTrue(Physics.Raycast(point + Vector3.up * 2f, Vector3.down, 4f, (1 << 8) | (1 << 9)), "Nothing to stand on at " + point);
        }
    }
}
