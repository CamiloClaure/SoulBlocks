using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SoulBlocks.Tests
{
    public class SpiritDialoguePlayModeTests
    {
        SpiritDialogue[] spirits;
        SoulPlayer player;
        [UnitySetUp]
        public IEnumerator Setup()
        {
            SceneManager.LoadScene(GameRoot.SceneName);
            for (int i = 0; i < 15; i++) yield return null;
            StartMenu.Dismiss();
            player = GameRoot.Instance.Player;
            player.readInput = false;
            spirits = Object.FindObjectsByType<SpiritDialogue>(FindObjectsSortMode.None);
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (SpiritDialogue.Active != null) SpiritDialogue.Active.Close();
            Time.timeScale = 1;
            Keys.Blocked = false;
            var empty = SceneManager.CreateScene("Dialogue cleanup");
            SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync(GameRoot.SceneName);
        }

        [UnityTest]
        public IEnumerator FiveSpiritsSpeakAndReleaseControls()
        {
            Assert.AreEqual(5, spirits.Length);
            Assert.AreEqual(5, spirits.Select(s => s.color).Distinct().Count());
            foreach (var spirit in spirits)
            {
                Assert.IsTrue(spirit.visual.GetComponentInChildren<MeshFilter>().sharedMesh.name.ToLower().Contains("sphere"));
                Assert.IsNull(spirit.GetComponent<WanderingSpirit>());
                player.Teleport(spirit.transform.position + Vector3.back);
                Assert.IsTrue(spirit.TryBegin(player));
                Assert.AreEqual("Hola, soy " + spirit.speakerName.ToLower() + ".", spirit.CurrentText);
                Assert.IsTrue(Keys.Blocked);
                Assert.IsTrue(player.MovementLocked);
                Assert.AreEqual(0, Time.timeScale);
                Assert.IsFalse(spirits.First(s => s != spirit).TryBegin(player));
                spirit.Advance();
                Assert.IsNull(SpiritDialogue.Active);
                Assert.IsFalse(Keys.Blocked);
                Assert.IsFalse(player.MovementLocked);
                Assert.AreEqual(1, Time.timeScale);
                Assert.IsTrue(SpiritDialogue.HandledInputThisFrame);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator DuplicateHasIndependentLinesAndDisablingCloses()
        {
            var original = spirits[0];
            var duplicate = Object.Instantiate(original);
            duplicate.transform.position = original.transform.position + Vector3.right * 2;
            duplicate.lines[0].text = "Mi diálogo propio.";
            Assert.AreNotEqual(duplicate.lines[0].text, original.lines[0].text);
            duplicate.lines = new[] { new SpiritDialogueLine { text = "Primera frase" }, new SpiritDialogueLine { text = "Segunda frase" } };
            player.Teleport(duplicate.transform.position + Vector3.back);
            Assert.IsTrue(duplicate.TryBegin(player));
            Assert.AreEqual("Primera frase", duplicate.CurrentText);
            duplicate.Advance();
            Assert.AreEqual("Segunda frase", duplicate.CurrentText);
            duplicate.gameObject.SetActive(false);
            Assert.IsNull(SpiritDialogue.Active);
            Assert.IsFalse(Keys.Blocked);
            Assert.AreEqual(1, Time.timeScale);
            player.Teleport(original.transform.position + Vector3.right * 20);
            Assert.IsFalse(original.TryBegin(player));
            player.Teleport(original.transform.position);
            original.lines = new SpiritDialogueLine[0];
            Assert.IsFalse(original.TryBegin(player));
            Assert.IsFalse(Keys.Blocked);
            Object.Destroy(duplicate.gameObject);
            yield return null;
        }
    }
}
