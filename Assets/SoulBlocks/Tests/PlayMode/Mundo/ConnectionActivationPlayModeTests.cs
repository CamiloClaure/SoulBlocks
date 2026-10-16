using System.Linq;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SoulBlocks.Tests
{
    // MapBuilder (og/mapa, verified here not rebuilt) already activates any tile whose
    // requiresUnlock matches the granted id; og/mundo only adds the toast on top. This exercises
    // the activation itself so a regression there is not silently missed.
    public class ConnectionActivationPlayModeTests
    {
        [UnityTest]
        public IEnumerator ConnectionTilesActivateOnRaiz2()
        {
            SceneManager.LoadScene("SoulBlocks");
            for (int i = 0; i < 10; i++) yield return null;

            var root = GameRoot.Instance;
            Assert.IsNotNull(root, "GameRoot did not boot.");
            Assert.Greater(root.Layout.connections.Count, 0, "The scene has no connection path.");
            Assert.AreEqual(UnlockIds.Raiz2, root.Layout.connections[0].requiresUnlock);
            var bridge = Object.FindObjectsByType<AuthoredPath>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(p => p.role == PathRole.Connection);
            Assert.IsFalse(bridge.gameObject.activeInHierarchy, "The connection path should start locked.");

            GameEvents.RaiseUnlockGranted(UnlockIds.Raiz2);
            for (int i = 0; i < 3; i++) yield return null;

            Assert.IsTrue(bridge.gameObject.activeInHierarchy, "The connection path did not open after Raiz2 was granted.");
        }
    }
}
