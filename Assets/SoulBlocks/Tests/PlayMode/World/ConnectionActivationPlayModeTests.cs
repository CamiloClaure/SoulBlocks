using System.Linq;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SoulBlocks.Tests
{
    // The connection path sits behind an UnlockGate: granting Raiz2 must open it (PortalSystem
    // only adds the toast on top).
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
