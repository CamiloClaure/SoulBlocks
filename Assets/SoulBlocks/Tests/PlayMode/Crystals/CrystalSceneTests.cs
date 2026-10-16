using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SoulBlocks.Tests
{
    public class CrystalSceneTests
    {
        [UnityTest]
        public IEnumerator AuthoredCrystalCanBeMinedByActualPlayer()
        {
            SaveSystem.PathOverride = Path.Combine(Application.temporaryCachePath, "crystal-review-save.json");
            SceneManager.LoadScene(GameRoot.SceneName);
            for (int i = 0; i < 10; i++) yield return null;
            StartMenu.Dismiss();
            var root = GameRoot.Instance;
            Assert.IsNotNull(root);
            if (SpecialBlockSystem.Instance != null) SpecialBlockSystem.Instance.PassivesEnabled = false;
            var crystal = GameObject.Find("Crystal Yellow Small").GetComponent<BreakableCrystal>();
            Assert.IsNotNull(crystal);
            root.Player.readInput = false;
            root.Player.Teleport(crystal.transform.position + Vector3.back * 1.2f + Vector3.up * .03f);
            var attack = root.Player.GetComponent<CrystalMiningAttack>();
            Assert.IsNotNull(attack);
            root.Player.FaceDirection(Vector3.forward);
            yield return null;
            root.ViewCamera.GetComponent<SoulCamera>().SnapToPlayer();
            Capture(crystal.transform.position, "camera-behind-player.png", root.ViewCamera);
            Capture(crystal.transform.position, "crystal-ready.png");
            for (int i = 0; i < 10; i++)
            {
                Assert.IsTrue(attack.TrySwing(Vector3.forward));
                // Abilities must not interrupt or consume charges during the mining swing.
                Assert.IsFalse(root.GetComponent<DashAbility>().TryDash(Vector3.forward));
                yield return new WaitForSeconds(.25f);
                Assert.AreEqual(9 - i, crystal.Health);
                if (i == 0)
                {
                    var tint = new MaterialPropertyBlock();
                    crystal.model.GetComponentInChildren<Renderer>().GetPropertyBlock(tint);
                    Assert.AreEqual(Color.red, tint.GetColor("_BaseColor"), "The authored model must flash red too.");
                    yield return null; // Let LateUpdate apply the impact pose before rendering it.
                    Capture(crystal.transform.position, "crystal-impact.png");
                }
                yield return new WaitForSeconds(.35f);
            }
            Assert.IsTrue(crystal == null);
        }

        [UnityTest]
        public IEnumerator WizardHoldsThePickaxeAndSwingsItsOwnArm()
        {
            SaveSystem.PathOverride = Path.Combine(Application.temporaryCachePath, "crystal-review-save.json");
            SceneManager.LoadScene(GameRoot.SceneName);
            for (int i = 0; i < 10; i++) yield return null;
            StartMenu.Dismiss();
            var player = GameRoot.Instance.Player;
            var pickaxe = player.GetComponent<CrystalMiningAttack>().pickaxe.transform;
            player.RightArm(out var upper, out var fore, out var hand);
            Assert.IsNotNull(upper);
            Assert.IsNotNull(fore);
            Assert.AreEqual("Hand.R", hand.name, "The wizard is the default model, so the swing poses its arm.");
            Assert.AreEqual(hand, pickaxe.parent, "The pickaxe moves from the hidden classic rig to the wizard's hand.");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // Destroy the test scene before releasing the temporary save path.
            var empty = SceneManager.CreateScene("Crystal Test Cleanup");
            SceneManager.SetActiveScene(empty);
            var old = SceneManager.GetSceneByName(GameRoot.SceneName);
            if (old.IsValid()) yield return SceneManager.UnloadSceneAsync(old);
            SaveSystem.PathOverride = null;
            Keys.Blocked = false;
            Time.timeScale = 1;
        }

        static void Capture(Vector3 focus, string filename, Camera gameplayCamera = null)
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            var go = gameplayCamera == null ? new GameObject("Crystal verification camera") : null;
            var camera = gameplayCamera != null ? gameplayCamera : go.AddComponent<Camera>();
            if (gameplayCamera == null)
            {
                camera.transform.position = focus + new Vector3(3.2f, 2.4f, -4.2f);
                camera.transform.LookAt(focus + new Vector3(-.2f, .5f, -.4f));
                camera.fieldOfView = 42;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.05f, .05f, .09f);
            }
            var target = new RenderTexture(960, 720, 24);
            var previous = RenderTexture.active;
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var image = new Texture2D(960, 720, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 960, 720), 0, 0);
            image.Apply();
            Directory.CreateDirectory("Validation");
            File.WriteAllBytes(Path.Combine("Validation", filename), image.EncodeToPNG());
            RenderTexture.active = previous;
            camera.targetTexture = null;
            Object.Destroy(image);
            Object.Destroy(target);
            if (go != null) Object.Destroy(go);
        }
    }
}
