using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine;

namespace SoulBlocks.Tests
{
    public class SceneIntegrityTests
    {
        const string ScenePath = "Assets/Scenes/SoulBlocks.unity";

        [Test]
        public void SceneHasNoMissingScriptsAndExpectedRoots()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();

            foreach (var root in roots)
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    Assert.AreEqual(0, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject), "Missing script on " + t.name);

            Assert.IsTrue(roots.Any(r => r.name == "Main Island"), "Missing Main Island root.");
            Assert.IsTrue(roots.Any(r => r.GetComponent<CentralSpiritTree>() != null), "Missing the central tree root.");
            Assert.IsTrue(roots.Any(r => r.GetComponentInChildren<Terrain>() != null && r.name == GameRoot.IslandName), "The island terrain must sit under Main Island (HubZone).");
            Assert.IsTrue(roots.Any(r => r.GetComponent<SoulPlayer>() != null), "Missing Player root.");
        }
    }
}
