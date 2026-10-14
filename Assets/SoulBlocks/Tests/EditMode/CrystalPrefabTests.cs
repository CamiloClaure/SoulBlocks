using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SoulBlocks.Tests
{
    public class CrystalPrefabTests
    {
        [Test]
        public void PrefabContainsReplaceableModelAndSupportsThreeHitYellowCrystal()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SoulBlocks/Prefabs/Crystals/Crystal.prefab");
            Assert.IsNotNull(prefab);
            var instance = Object.Instantiate(prefab);
            try
            {
                var crystal = instance.GetComponent<BreakableCrystal>();
                crystal.color = CrystalColor.Yellow;
                crystal.size = CrystalSize.Small;
                crystal.ConfigureAppearance();
                Assert.AreEqual(3, crystal.MaxHealth);
                Assert.AreEqual("Model", crystal.model.name);
                Assert.AreEqual(.9f, instance.GetComponent<BoxCollider>().size.y, .001f);
            }
            finally { Object.DestroyImmediate(instance); }
        }

        [Test]
        public void SceneHasAuthoredPrefabBesideTreeAndWiredMiningPlayer()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SoulBlocks.unity");
            var crystal = GameObject.Find("Crystal Yellow - Tree").GetComponent<BreakableCrystal>();
            var player = Object.FindAnyObjectByType<SoulPlayer>();
            var tree = GameObject.Find("Wisdom Tree");
            Assert.IsNotNull(crystal);
            Assert.IsTrue(PrefabUtility.IsPartOfPrefabInstance(crystal));
            Assert.Less(Vector3.Distance(tree.transform.position, crystal.transform.position), 4f);
            Assert.AreEqual(player.GetComponent<CharacterController>().height * .5f,
                crystal.GetComponent<BoxCollider>().size.y, .001f);
            var attack = player.GetComponent<CrystalMiningAttack>();
            Assert.IsNotNull(attack);
            Assert.IsNotNull(attack.pickaxe);
            Assert.IsNotNull(attack.hitbox);
            Assert.IsFalse(attack.hitbox.enabled);
            Assert.IsTrue(attack.hitbox.isTrigger);
        }
    }
}
