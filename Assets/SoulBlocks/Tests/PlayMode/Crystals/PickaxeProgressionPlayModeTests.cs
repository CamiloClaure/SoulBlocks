using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SoulBlocks.Tests
{
    public class PickaxeProgressionPlayModeTests
    {
        [TestCase(1, 1)]
        [TestCase(10, 10)]
        [TestCase(11, 12)]
        [TestCase(20, 30)]
        [TestCase(30, 60)]
        [TestCase(40, 110)]
        [TestCase(50, 180)]
        public void DamageAccumulatesAcrossStages(int level, int expected) => Assert.AreEqual(expected, MiningBalance.Current.Damage(level));
        [Test]
        public void SpeedPartiallyResetsAtEachNewColor()
        {
            for (int stage = 1; stage < 5; stage++)
            {
                int end = stage * 10;
                Assert.Greater(MiningBalance.Current.Seconds(end + 1), MiningBalance.Current.Seconds(end));
                Assert.Less(MiningBalance.Current.Seconds(end + 1), MiningBalance.Current.Seconds(end - 9));
            }
            Assert.AreEqual(50, SaveStore.Deserialize(SaveStore.Serialize(new RunState { pickaxeLevel = 50 })).pickaxeLevel);
        }

        [UnityTest]
        public IEnumerator PurchasesSwapModelsAndMaxLevelAppliesDamageOnce()
        {
            Time.timeScale = 1;
            Keys.Blocked = false;
            SceneManager.LoadScene(GameRoot.SceneName);
            for (int i = 0; i < 15; i++)
                yield return null;
            StartMenu.Dismiss();
            var root = GameRoot.Instance;
            root.Player.readInput = false;
            SpecialBlockSystem.Instance.PassivesEnabled = false;
            var mining = root.Player.GetComponent<CrystalMiningAttack>();
            var view = mining.pickaxe.GetComponent<PickaxeVisual>();
            Assert.IsNotNull(view);
            Assert.AreEqual(5, view.stagePrefabs.Length);
            root.Run.collectedCrystals = 0;
            Assert.IsFalse(mining.UpgradePickaxe());
            Assert.AreEqual(1, mining.Level);
            root.Run.collectedCrystals = 100000;
            for (int i = 1; i < 50; i++)
            {
                int before = root.Run.collectedCrystals;
                Assert.IsTrue(mining.UpgradePickaxe());
                Assert.AreEqual(before - MiningBalance.Current.Cost(i), root.Run.collectedCrystals);
                yield return null;
                Assert.AreEqual(1, view.modelSocket.childCount);
                Assert.IsTrue(view.modelSocket.GetChild(0).name.StartsWith(view.stagePrefabs[MiningBalance.Stage(i + 1)].name));
            }
            int remaining = root.Run.collectedCrystals;
            Assert.IsFalse(mining.UpgradePickaxe());
            Assert.AreEqual(remaining, root.Run.collectedCrystals);
            root.Player.Teleport(root.Tree.position + Vector3.left * 10);
            yield return null;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.position = root.Player.transform.position + Vector3.forward * 1.2f;
            var crystal = go.AddComponent<BreakableCrystal>();
            crystal.color = CrystalColor.Red;
            crystal.ResetCrystal();
            go.AddComponent<BoxCollider>();
            Physics.SyncTransforms();
            Assert.IsTrue(mining.TrySwing(Vector3.forward));
            yield return new WaitForSeconds(mining.Duration + .08f);
            Assert.AreEqual(2820, crystal.Health);
            Assert.IsFalse(mining.IsSwinging);
            Object.Destroy(go);
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1;
            Keys.Blocked = false;
            var old = SceneManager.GetSceneByName(GameRoot.SceneName);
            if (old.IsValid())
            {
                var empty = SceneManager.CreateScene("Mining cleanup");
                SceneManager.SetActiveScene(empty);
                yield return SceneManager.UnloadSceneAsync(old);
            }
        }
    }
}
