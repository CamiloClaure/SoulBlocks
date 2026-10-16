using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SoulBlocks.Tests
{
    public class NutritionTests
    {
        SpecialBlockSystem system;
        SoulPlayer player;
        [UnitySetUp] public IEnumerator Setup()
        {
            Time.timeScale = 1; Keys.Blocked = false;
            SceneManager.LoadScene("WorldBuilding");
            for (int i = 0; i < 15; i++) yield return null;
            system = SpecialBlockSystem.Instance; player = GameRoot.Instance.Player;
            player.readInput = false; system.PassivesEnabled = false;
            Object.FindAnyObjectByType<GuidingTorch>().SetLit(false);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale = 1; Keys.Blocked = false;
            var empty = SceneManager.CreateScene("Nutrition cleanup");
            SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("WorldBuilding");
        }
        [UnityTest] public IEnumerator YellowPassiveTravelsWithPlayerAndRevivesNearbyTree()
        {
            var tree = NourishableTree.All.First();
            Assert.IsFalse(tree.Alive);
            player.Teleport(tree.transform.position + Vector3.right * 10);
            system.Select((int)SpecialBlock.Red);
            system.PassivesEnabled = true;
            system.RandomValue = () => 0;
            system.NotifyMiningHit(Vector3.one * 1000, false);
            yield return null;
            Assert.IsFalse(tree.Alive, "A distant tree must not receive the passive light.");
            player.Teleport(tree.transform.position + Vector3.right * 2);
            yield return null;
            Assert.IsTrue(tree.Alive, "The light follows the player even with red selected.");
            var tint = new MaterialPropertyBlock();
            tree.model.GetComponentInChildren<Renderer>().GetPropertyBlock(tint);
            Assert.AreEqual(Color.yellow, tint.GetColor("_BaseColor"));
            system.ResetLab();
            Assert.IsFalse(tree.Alive);
        }
        [UnityTest] public IEnumerator YellowActiveRevivesAllNearbyTreesAndSpawnsBoundedSpirits()
        {
            var tree = NourishableTree.All.First();
            tree.spiritInterval = 1; tree.maxSpirits = 2;
            var second = Object.Instantiate(tree, tree.transform.position + Vector3.right * 2, Quaternion.identity);
            second.spiritInterval = 1; second.maxSpirits = 2;
            var distant = Object.Instantiate(tree, tree.transform.position + Vector3.right * 100, Quaternion.identity);
            player.Teleport(tree.transform.position + Vector3.back * 1.5f);
            GameRoot.Instance.ViewCamera.transform.rotation = Quaternion.Euler(15, 0, 0);
            system.Select((int)SpecialBlock.Yellow);
            Assert.IsTrue(system.TryUseSelected());
            Assert.IsTrue(tree.Alive); Assert.IsTrue(second.Alive); Assert.IsFalse(distant.Alive);
            yield return new WaitForSeconds(3.3f);
            var generated = Object.FindObjectsByType<WanderingSpirit>().Where(s => s.name == "Alma del árbol nutrido").ToArray();
            Assert.AreEqual(4, generated.Length, "Each tree is capped at two live spirits in this test.");
            Assert.IsTrue(generated.All(s => s.torch != null && s.visual != null));
            Assert.Less(Vector3.Distance(generated[0].transform.position, tree.transform.position), 8);
            system.ResetLab();
            yield return null;
            Assert.IsFalse(tree.Alive);
            Assert.AreEqual(0, Object.FindObjectsByType<WanderingSpirit>().Count(s => s.name == "Alma del árbol nutrido"));
        }
        [UnityTest] public IEnumerator BlueAuraRollsBonusOnceOnBreakAndResetClearsIt()
        {
            var crystal = BreakableCrystal.All.First(c => c.Available && c.size == CrystalSize.Small);
            player.Teleport(crystal.transform.position + Vector3.right * 20);
            system.PassivesEnabled = true; system.RandomValue = () => 0;
            system.Select((int)SpecialBlock.Red);
            Assert.IsTrue(system.TryNourishCrystal(crystal, true));
            Assert.IsFalse(system.TryNourishCrystal(crystal, true), "An aura cannot stack on repeated hits.");
            int before = crystal.Health;
            Assert.AreEqual(crystal.MaxHealth, before);
            Assert.IsNotNull(crystal.transform.Find("Aura de recompensa azul"));
            crystal.TakeDamage(1000);
            Assert.AreEqual(5, crystal.FragmentReward, "3 + rounded-up 50% bonus.");
            Assert.IsFalse(crystal.TakeDamage(1000));
            Assert.AreEqual(5, Object.FindObjectsByType<CrystalFragment>().Length);
            yield return null;
            var other = BreakableCrystal.All.First(c => c.Available && c.size == CrystalSize.Small);
            other.Nourish(.5f, .5f); system.RandomValue = () => 1;
            other.TakeDamage(1000);
            Assert.AreEqual(3, other.FragmentReward, "Aura is a chance, not a guaranteed bonus.");
            var reset = BreakableCrystal.All.First(c => c.Available);
            reset.Nourish(.5f, .5f); reset.ResetCrystal();
            Assert.IsFalse(reset.Nourished);
        }
        [UnityTest] public IEnumerator TreesRemainAliveAfterTheOriginalLightExpires()
        {
            var tree = NourishableTree.All.First();
            tree.Revive(.1f);
            Assert.IsTrue(tree.Alive);
            yield return new WaitForSeconds(.2f);
            Assert.IsTrue(tree.Alive);
            var tint = new MaterialPropertyBlock();
            tree.model.GetComponentInChildren<Renderer>().GetPropertyBlock(tint);
            Assert.AreEqual(Color.yellow, tint.GetColor("_BaseColor"));
            tree.ResetTree();
            Assert.IsFalse(tree.Alive, "Only an explicit test reset extinguishes a revived tree.");
        }
    }
}
