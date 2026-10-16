using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SoulBlocks.Tests
{
    public class SpiritGuidingPlayModeTests
    {
        GuidingTorch torch;
        SoulPlayer player;
        AuthoredPath path;
        [UnitySetUp]
        public IEnumerator Setup()
        {
            SceneManager.LoadScene(GameRoot.SceneName);
            for (int i = 0; i < 15; i++)
                yield return null;
            StartMenu.Dismiss();
            torch = Object.FindAnyObjectByType<GuidingTorch>();
            player = GameRoot.Instance.Player;
            player.readInput = false;
            path = GameObject.Find("Primary Path").GetComponent<AuthoredPath>();
            Assert.IsNotNull(torch);
            Assert.GreaterOrEqual(Object.FindObjectsByType<WanderingSpirit>(FindObjectsSortMode.None).Length, 18);
            if (SpecialBlockSystem.Instance != null) SpecialBlockSystem.Instance.PassivesEnabled = false;
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1;
            Keys.Blocked = false;
            var empty = SceneManager.CreateScene("Spirit cleanup");
            SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync(GameRoot.SceneName);
        }

        [UnityTest]
        public IEnumerator CrystalsDoNotReleaseOldSoulPickups()
        {
            Assert.AreEqual(0, Object.FindObjectsByType<WanderingSpirit>(FindObjectsSortMode.None).Count(s => s.name.StartsWith("Released Soul")));
            int before = GameRoot.Instance.Run.collectedCrystals;
            var crystal = Object.FindObjectsByType<BreakableCrystal>(FindObjectsSortMode.None).First(c => c.Available);
            int reward = 3 * (int)crystal.size;
            var color = crystal.color;
            player.Teleport(crystal.transform.position + Vector3.right * 15);
            Assert.IsTrue(crystal.TakeDamage(crystal.MaxHealth));
            Assert.IsFalse(crystal.TakeDamage(crystal.MaxHealth), "A broken crystal must not spawn twice");
            yield return new WaitForSeconds(.8f);
            Assert.AreEqual(before, GameRoot.Instance.Run.collectedCrystals, "Breaking is not collecting");
            var fragments = Object.FindObjectsByType<CrystalFragment>(FindObjectsSortMode.None);
            Assert.AreEqual(reward, fragments.Length);
            foreach (var fragment in fragments)
            {
                Assert.AreEqual(color, fragment.Color);
                Assert.IsNotNull(fragment.model);
                Assert.IsFalse(fragment.TryCollect(player), "Cannot collect from far away");
            }
            Keys.Blocked = true;
            player.Teleport(fragments[0].transform.position);
            Assert.IsFalse(fragments[0].TryCollect(player));
            Keys.Blocked = false;
            foreach (var fragment in fragments)
            {
                player.Teleport(fragment.transform.position);
                Assert.IsTrue(fragment.TryCollect(player));
                Assert.IsFalse(fragment.TryCollect(player), "Cannot count twice");
            }
            Assert.AreEqual(before + reward, GameRoot.Instance.Run.collectedCrystals);
            yield return null;
            Assert.AreEqual(0, Object.FindObjectsByType<CrystalFragment>(FindObjectsSortMode.None).Length);
            foreach (var spirit in Object.FindObjectsByType<WanderingSpirit>(FindObjectsSortMode.None))
            {
                Assert.AreEqual(1, spirit.visual.childCount);
                Assert.GreaterOrEqual(spirit.wanderSpeed, 4);
                Assert.Greater(spirit.followSpeed, player.moveSpeed);
                Assert.IsTrue(spirit.visual.GetComponentInChildren<Renderer>().sharedMaterial.IsKeywordEnabled("_EMISSION"));
            }
        }

        [UnityTest]
        public IEnumerator TorchCanBeCarriedPlacedRetrievedAndDocked()
        {
            Assert.IsFalse(torch.Lit);
            player.Teleport(torch.AltarPosition + Vector3.right * 1.7f);
            Assert.IsTrue(torch.Interact(player));
            yield return null;
            yield return null;
            Assert.AreEqual(TorchState.Carried, torch.State);
            Assert.IsTrue(torch.Lit);
            Assert.AreEqual(player.TorchHand != null ? player.TorchHand : player.transform, torch.transform.parent);
            Assert.AreEqual(1, torch.transform.lossyScale.x, .02f);
            var mining = player.GetComponent<CrystalMiningAttack>();
            Assert.IsTrue(mining.TrySwing(Vector3.forward));
            yield return new WaitForSeconds(.6f);
            Assert.AreEqual(TorchState.Carried, torch.State);
            Assert.AreEqual(player.TorchHand != null ? player.TorchHand : player.transform, torch.transform.parent);
            player.Teleport(path.transform.TransformPoint(path.Evaluate(.25f)) + Vector3.up * .1f);
            yield return null;
            Assert.IsTrue(torch.Interact(player));
            Assert.AreEqual(TorchState.Placed, torch.State);
            Vector3 placed = torch.transform.position;
            player.Teleport(player.transform.position + Vector3.back * 5);
            yield return new WaitForSeconds(.3f);
            Assert.Less(Vector3.Distance(Vector3.ProjectOnPlane(placed, Vector3.up), Vector3.ProjectOnPlane(torch.transform.position, Vector3.up)), .01f);
            Assert.IsFalse(torch.Interact(player), "Cannot retrieve at a distance");
            Capture();
            player.Teleport(placed + Vector3.right);
            Assert.IsTrue(torch.Interact(player));
            yield return null;
            player.Teleport(torch.AltarPosition + Vector3.right * 1.7f);
            Assert.IsTrue(torch.Interact(player));
            yield return null;
            Assert.AreEqual(TorchState.Altar, torch.State);
            Assert.IsTrue(torch.Lit);
            Assert.AreEqual(1, Object.FindObjectsByType<GuidingTorch>(FindObjectsSortMode.None).Length);
        }

        [UnityTest]
        public IEnumerator LightAttractsAndAltarReceivesExactlyOnce()
        {
            var spirit = Object.FindObjectsByType<WanderingSpirit>(FindObjectsSortMode.None).First();
            foreach (var other in Object.FindObjectsByType<WanderingSpirit>(FindObjectsSortMode.None))
                if (other != spirit) other.gameObject.SetActive(false);
            player.Teleport(torch.AltarPosition + Vector3.right * 1.7f);
            torch.Interact(player);
            player.Teleport(path.transform.TransformPoint(path.Evaluate(.3f)) + Vector3.up * .1f);
            yield return null;
            Assert.IsTrue(torch.Interact(player));
            torch.TickCall(12);
            spirit.transform.position = torch.transform.position + Vector3.right * 5;
            float before = Vector3.Distance(spirit.transform.position, torch.transform.position);
            yield return new WaitForSeconds(.5f);
            Assert.IsTrue(spirit.Following);
            Assert.Less(Vector3.Distance(spirit.transform.position, torch.transform.position), before);
            torch.SetLit(false);
            yield return null;
            Assert.IsFalse(spirit.Following);
            torch.SetLit(true);
            yield return null;
            Assert.IsTrue(spirit.Following);
            Keys.Blocked = true;
            var stopped = spirit.transform.position;
            yield return new WaitForSeconds(.2f);
            Assert.AreEqual(stopped, spirit.transform.position);
            Keys.Blocked = false;
            player.Teleport(torch.transform.position + Vector3.right);
            torch.Interact(player);
            yield return null;
            player.Teleport(torch.AltarPosition + Vector3.right * 1.7f);
            torch.Interact(player);
            spirit.transform.position = torch.AltarPosition + Vector3.forward;
            int souls = GameRoot.Instance.Run.reserveSouls;
            yield return new WaitForSeconds(1.8f);
            Assert.IsTrue(spirit == null);
            Assert.AreEqual(1, torch.GuidedSouls);
            Assert.AreEqual(souls + 1, GameRoot.Instance.Run.reserveSouls);
            yield return new WaitForSeconds(.2f);
            Assert.AreEqual(1, torch.GuidedSouls);
        }

        [UnityTest]
        public IEnumerator PlacedTorchGrowsInPulsesAndResetsWhenPlacedAgain()
        {
            player.Teleport(torch.AltarPosition + Vector3.right * 1.7f);
            torch.Interact(player);
            torch.TickCall(30);
            Assert.AreEqual(4, torch.CurrentRadius);
            player.Teleport(path.transform.TransformPoint(path.Evaluate(.65f)) + Vector3.up * .1f);
            yield return null;
            Assert.IsTrue(torch.Interact(player));
            Assert.AreEqual(4, torch.CurrentRadius);
            torch.TickCall(2);
            Assert.AreEqual(4, torch.CurrentRadius);
            torch.TickCall(1);
            Assert.AreEqual(6, torch.CurrentRadius);
            torch.SetLit(false);
            torch.TickCall(50);
            Assert.AreEqual(6, torch.CurrentRadius);
            torch.SetLit(true);
            torch.TickCall(9);
            Assert.AreEqual(12, torch.CurrentRadius);
            Assert.IsTrue(torch.ReadyToRetrieve);
            torch.TickCall(100);
            Assert.AreEqual(12, torch.CurrentRadius);
            Assert.IsTrue(torch.Calls(torch.LightPosition + Vector3.right * 10, false));
            player.Teleport(torch.transform.position + Vector3.right);
            Assert.IsTrue(torch.Interact(player));
            Assert.IsFalse(torch.ReadyToRetrieve);
            Assert.AreEqual(4, torch.CurrentRadius);
            Assert.IsTrue(torch.Calls(torch.LightPosition + Vector3.right * 10, true), "Collected spirits retain follow range");
            Assert.IsFalse(torch.Calls(torch.LightPosition + Vector3.right * 10, false));
            Assert.IsTrue(torch.Interact(player));
            Assert.AreEqual(4, torch.CurrentRadius);
        }

        [UnityTest]
        public IEnumerator WanderingSpheresStayOutsideTheRoad()
        {
            yield return new WaitForSeconds(1);
            foreach (var spirit in Object.FindObjectsByType<WanderingSpirit>(FindObjectsSortMode.None))
            {
                Assert.IsTrue(spirit.visual.GetComponentInChildren<MeshFilter>().sharedMesh.name.ToLower().Contains("sphere"));
                for (int i = 0; i <= 500; i++)
                {
                    var delta = path.transform.TransformPoint(path.Evaluate(i / 500f)) - spirit.transform.position;
                    delta.y = 0;
                    Assert.Greater(delta.magnitude, path.width * .5f, "An idle spirit entered the road");
                }
            }
        }

        [UnityTest]
        public IEnumerator PopulationFollowsDrawnPathsAndBlockLabIsAvailable()
        {
            var population = Object.FindAnyObjectByType<PathPopulation>();
            Assert.Greater(population.SpiritCount, 18);
            Assert.Greater(population.CrystalCount, 10);
            int count = population.SpiritCount;
            population.Populate();
            Assert.AreEqual(count, population.SpiritCount);
            var system = SpecialBlockSystem.Instance;
            Assert.IsTrue(system.Ready);
            Assert.AreEqual(5, system.Lab.blockDisplays.Length);
            Assert.AreEqual(15, system.Lab.slots.Length);
            Assert.Greater(Vector3.Distance(system.Lab.transform.position, GameRoot.Instance.Tree.position), 30);
            player.Teleport(system.Lab.transform.position + Vector3.up * .1f);
            for (int i = 0; i < 5; i++)
            { system.Select(i); Assert.IsTrue(system.TryUseSelected(), "Block " + i); }
            Assert.Greater(system.HelperCount, 0);
            Assert.AreEqual(1, system.ActiveBombs);
            system.ResetLab();
            yield return null;
            Assert.AreEqual(15, system.Lab.slots.Count(s => s.crystal != null));
            var extra = new GameObject("Additional drawn road");
            extra.transform.position = new Vector3(500, 5, 500);
            var extraPath = extra.AddComponent<AuthoredPath>();
            extraPath.Rebuild();
            var host = new GameObject("Extra population");
            var second = host.AddComponent<PathPopulation>();
            second.spiritPrefab = population.spiritPrefab;
            second.crystalPrefab = population.crystalPrefab;
            second.torch = torch;
            second.Populate();
            yield return null;
            Assert.IsTrue(second.GetComponentsInChildren<WanderingSpirit>().Any(s => Vector3.Distance(s.transform.position, extra.transform.position) < 40));
            Object.Destroy(host);
            Object.Destroy(extra);
        }

        [UnityTest]
        public IEnumerator CyanHelperCollectsCrystalFragments()
        {
            var system = SpecialBlockSystem.Instance;
            Vector3 point = system.Lab.transform.position;
            player.Teleport(torch.AltarPosition + Vector3.right * 2);
            int before = GameRoot.Instance.Run.collectedCrystals;
            var fragment = Object.Instantiate(Resources.Load<CrystalFragment>("Crystals/CrystalFragment"));
            fragment.Initialize(point + Vector3.up * .2f, point + Vector3.up * .15f, CrystalColor.Yellow, Color.yellow);
            Assert.IsTrue(system.SpawnHelper(point + Vector3.right * .5f));
            yield return new WaitForSeconds(1.2f);
            Assert.IsTrue(fragment == null);
            Assert.AreEqual(before + 1, GameRoot.Instance.Run.collectedCrystals);
        }

        void Capture()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            var go = new GameObject("Torch preview");
            var camera = go.AddComponent<Camera>();
            camera.transform.position = torch.transform.position + new Vector3(7, 5, -7);
            camera.transform.LookAt(torch.transform.position);
            camera.fieldOfView = 55;
            var target = new RenderTexture(1200, 900, 24);
            var previous = RenderTexture.active;
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var image = new Texture2D(1200, 900, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1200, 900), 0, 0);
            image.Apply();
            System.IO.Directory.CreateDirectory("Logs/Captures");
            System.IO.File.WriteAllBytes("Logs/Captures/torch-guide.png", image.EncodeToPNG());
            RenderTexture.active = previous;
            camera.targetTexture = null;
            Object.Destroy(image);
            Object.Destroy(target);
            Object.Destroy(go);
        }
    }
}
