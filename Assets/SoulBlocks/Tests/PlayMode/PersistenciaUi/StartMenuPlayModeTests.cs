using System.Collections;
using NUnit.Framework;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SoulBlocks.Tests
{
    public class StartMenuPlayModeTests
    {
        [UnityTest]
        public IEnumerator FirstBootOpensModalAndDismissRestoresInput()
        {
            bool mapBuilt = false;
            GameEvents.MapBuilt += _ => mapBuilt = true;

            StartMenu.ResetForTests(); // another test may have booted the scene first in this play session
            SceneManager.LoadScene("SoulBlocks");
            for (int i = 0; i < 10; i++) yield return null;
            Assert.IsTrue(mapBuilt, "MapBuilt was not raised.");

            Assert.IsTrue(Keys.Blocked, "Start menu should block input on the first boot of the process.");
            Assert.IsNotNull(GameRoot.Instance.Player, "Player missing from the booted scene.");
            Assert.IsTrue(GameRoot.Instance.Player.MovementLocked, "Player should be locked while the start menu is open.");

            StartMenu.Dismiss();
            yield return null;

            Assert.IsFalse(Keys.Blocked, "Dismiss should release Keys.Blocked.");
            Assert.IsFalse(GameRoot.Instance.Player.MovementLocked, "Dismiss should unlock the player.");
        }
    }
}
