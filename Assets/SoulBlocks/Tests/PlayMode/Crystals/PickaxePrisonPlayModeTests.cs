using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SoulBlocks.Tests
{
    // The pickaxe swing also chips prisons, at its color stage: a yellow pickaxe chips a T1 and
    // bounces off a T2 until it turns blue.
    public class PickaxePrisonPlayModeTests
    {
        [UnityTest]
        public IEnumerator SwingChipsPrisonsUpToItsColorStage()
        {
            SaveSystem.PathOverride = Path.Combine(Application.temporaryCachePath, "pickaxe-prison-save.json");
            SceneManager.LoadScene(GameRoot.SceneName);
            for (int i = 0; i < 10; i++) yield return null;
            StartMenu.Dismiss();
            var root = GameRoot.Instance;
            root.Player.readInput = false;
            SpecialBlockSystem.Instance.PassivesEnabled = false;
            var pickaxe = root.Player.GetComponent<CrystalMiningAttack>();

            var t1 = root.Layout.prisons.First(p => p.tier == PrisonTier.T1 && string.IsNullOrEmpty(p.requiresUnlock));
            var t2 = root.Layout.prisons.First(p => p.tier == PrisonTier.T2 && string.IsNullOrEmpty(p.requiresUnlock));
            root.Run.pickaxeLevel = 10; // still yellow

            yield return SwingAt(root, pickaxe, t2);
            Assert.AreEqual(0, root.Run.Prison(t2.id).damage, "A yellow pickaxe must not chip a T2.");
            StringAssert.Contains("requiere pico azul", root.GetComponent<PrisonSystem>().GetComponentsInChildren<PrisonView>().First(v => v.PrisonId == t2.id).Label);

            yield return SwingAt(root, pickaxe, t1);
            Assert.Greater(root.Run.Prison(t1.id).damage, 0, "The swing did not chip the T1 in front of it.");

            root.Run.pickaxeLevel = 11; // blue
            yield return SwingAt(root, pickaxe, t2);
            Assert.Greater(root.Run.Prison(t2.id).damage, 0, "A blue pickaxe must chip a T2.");
        }

        static IEnumerator SwingAt(GameRoot root, CrystalMiningAttack pickaxe, PrisonLayout prison)
        {
            var tile = root.Layout.tiles.Where(t => t.branchId == prison.branchId).OrderBy(t => (t.position - prison.position).sqrMagnitude).First();
            Vector3 toward = prison.position - tile.position;
            toward.y = 0f;
            if (toward.sqrMagnitude < .01f) toward = Vector3.forward;
            root.Player.Teleport(prison.position - toward.normalized * 1.1f);
            yield return null;
            Assert.IsTrue(pickaxe.TrySwing(toward));
            yield return new WaitForSeconds(pickaxe.Duration + .05f);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SaveSystem.PathOverride = null;
            yield return null;
        }
    }
}
