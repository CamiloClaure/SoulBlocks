using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SoulBlocks.Tests
{
    public class HudPlayModeTests
    {
        // Unity issue UUM-22819: UnloadUnusedAssets (run by every GameRoot.Restart scene load)
        // unloads runtime textures referenced only by a GUIStyle, and the HUD lost every box.
        [UnityTest]
        public IEnumerator SolidTexturesSurviveUnloadUnusedAssets()
        {
            var style = new GUIStyle();
            style.normal.background = Hud.Solid(Color.red);
            yield return Resources.UnloadUnusedAssets();
            Assert.IsTrue(style.normal.background != null, "The style's background texture was unloaded.");
        }
    }
}
