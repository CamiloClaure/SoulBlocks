using System.Collections;
using NUnit.Framework;
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
            if (root.Layout.connections.Count == 0)
            {
                Assert.Ignore("No connections were generated for this seed.");
                yield break;
            }

            var mapBuilder = root.GetComponent<MapBuilder>();
            Assert.IsNotNull(mapBuilder, "MapBuilder was not attached.");

            var connection = root.Layout.connections[0];
            Assert.Greater(connection.tiles.Count, 0, "Connection has no tiles.");
            var tileObj = mapBuilder.TileObject(connection.tiles[0]);
            Assert.IsNotNull(tileObj, "Connection tile object missing.");
            Assert.IsFalse(tileObj.activeSelf, "Connection tile should start locked.");

            GameEvents.RaiseUnlockGranted(UnlockIds.Raiz2);
            for (int i = 0; i < 3; i++) yield return null;

            Assert.IsTrue(tileObj.activeSelf, "Connection tile did not activate after Raiz2 was granted.");
        }
    }
}
