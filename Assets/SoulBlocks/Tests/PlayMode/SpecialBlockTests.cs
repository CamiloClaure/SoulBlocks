using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SoulBlocks.Tests
{
    public class SpecialBlockTests
    {
        SpecialBlockSystem system;
        GameRoot root;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveSystem.PathOverride = Path.Combine(Application.temporaryCachePath, "special-block-test.json");
            GameRoot.PendingLoad = new RunState { seed = 123 };
            SceneManager.LoadScene(GameRoot.SceneName);
            for (int i = 0; i < 10; i++) yield return null;
            StartMenu.Dismiss();
            root = GameRoot.Instance;
            root.Player.readInput = false;
            system = SpecialBlockSystem.Instance;
            Assert.IsNotNull(system);
            Assert.IsTrue(system.Ready);
            system.PassivesEnabled = false;
        }

        [UnityTest]
        public IEnumerator CaptureLabPreview()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) yield break;
            system.Reveal(system.Lab.tree.position, 30);
            yield return null;
            var go = new GameObject("Lab preview camera");
            var camera = go.AddComponent<Camera>();
            camera.transform.position = system.Lab.tree.position + new Vector3(12, 18, -22);
            camera.transform.LookAt(system.Lab.tree.position + Vector3.back * 3);
            camera.fieldOfView = 45;
            var target = new RenderTexture(1280, 900, 24);
            var previous = RenderTexture.active;
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var image = new Texture2D(1280, 900, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1280, 900), 0, 0);
            image.Apply();
            Directory.CreateDirectory("Validation");
            File.WriteAllBytes("Validation/special-block-lab.png", image.EncodeToPNG());
            RenderTexture.active = previous;
            camera.targetTexture = null;
            Object.Destroy(image);
            Object.Destroy(target);
            Object.Destroy(go);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (system != null && system.ShopOpen) system.CloseShop();
            var empty = SceneManager.CreateScene("Block Cleanup");
            SceneManager.SetActiveScene(empty);
            var scene = SceneManager.GetSceneByName(GameRoot.SceneName);
            if (scene.IsValid()) yield return SceneManager.UnloadSceneAsync(scene);
            SaveSystem.PathOverride = null;
            Keys.Blocked = false;
            Time.timeScale = 1;
        }

        [Test]
        public void DirectPowersActivateWithoutPriorSelectionAndRespectCooldownsAndMenus()
        {
            Keys.Blocked = true;
            Assert.IsFalse(system.TryUse(SpecialBlock.Red));
            Assert.AreEqual(SpecialBlock.Yellow, system.Selected);
            Keys.Blocked = false;
            for (int i = 0; i < 5; i++)
            {
                Assert.IsTrue(system.TryUse((SpecialBlock)i));
                Assert.AreEqual((SpecialBlock)i, system.Selected);
                Assert.IsFalse(system.TryUse((SpecialBlock)i));
            }
            Assert.AreEqual(1, system.ActiveBombs);
            Assert.Greater(system.HelperCount, 0);
        }

        [Test]
        public void LabContainsFiveColorsThreeSizesAndFiveTreeBlocks()
        {
            Assert.AreEqual(15, system.Lab.slots.Length);
            Assert.AreEqual(5, system.Lab.blockDisplays.Length);
            Assert.AreEqual(5, system.Lab.slots.Select(s => s.color).Distinct().Count());
            foreach (var group in system.Lab.slots.GroupBy(s => s.color))
                Assert.AreEqual(3, group.Select(s => s.size).Distinct().Count());
            Assert.AreEqual(MiningBalance.Current.Health(CrystalColor.Yellow), Crystal(CrystalColor.Yellow, CrystalSize.Small).Health);
            Assert.IsTrue(Crystal(CrystalColor.Blue, CrystalSize.Small).Available);
        }

        BreakableCrystal Crystal(CrystalColor color, CrystalSize size) => system.Lab.slots.First(s => s.color == color && s.size == size).crystal;
        void StandNear(Vector3 point)
        {
            root.Player.Teleport(point + Vector3.back * 1.5f + Vector3.up * .05f);
            root.ViewCamera.transform.rotation = Quaternion.Euler(15, 0, 0);
        }

        [UnityTest]
        public IEnumerator BluePulsesNourishWithoutDamage()
        {
            var crystal = Crystal(CrystalColor.Blue, CrystalSize.Small);
            StandNear(crystal.transform.position);
            system.RandomValue = () => 1;
            system.Select((int)SpecialBlock.Blue);
            Assert.IsTrue(system.TryUseSelected());
            Assert.IsFalse(system.TryUseSelected(), "Cooldown prevents repeated casts.");
            yield return null;
            Assert.IsTrue(crystal.Available);
            Assert.IsFalse(crystal.Nourished);
            int health = crystal.Health;
            system.RandomValue = () => 0;
            yield return new WaitForSeconds(1.2f);
            Assert.IsTrue(crystal.Nourished);
            Assert.AreEqual(health, crystal.Health);
        }

        [UnityTest]
        public IEnumerator GreenAcceleratesAndResetRemovesTheEffect()
        {
            StandNear(Crystal(CrystalColor.Green, CrystalSize.Small).transform.position);
            system.Select((int)SpecialBlock.Green);
            Assert.IsTrue(system.TryUseSelected());
            yield return null;
            Assert.Greater(SpecialBlockSystem.PlayerTempo, 1);
            Assert.Greater(root.Player.SpeedMultiplier, 1);
            system.ResetLab();
            yield return null;
            Assert.AreEqual(1f, SpecialBlockSystem.PlayerTempo);
            Assert.AreEqual(1f, root.Player.SpeedMultiplier, .001f);
        }

        [UnityTest]
        public IEnumerator RedBombWaitsForFuseThenDamagesOnce()
        {
            var crystal = Crystal(CrystalColor.Red, CrystalSize.Large);
            StandNear(crystal.transform.position);
            int before = crystal.Health;
            system.Select((int)SpecialBlock.Red);
            Assert.IsTrue(system.TryUseSelected());
            Assert.AreEqual(1, system.ActiveBombs);
            yield return new WaitForSeconds(1);
            Assert.AreEqual(before, crystal.Health);
            Assert.AreEqual(1, system.ActiveBombs, "Still on its fuse (the countdown is a Hud world label).");
            yield return new WaitForSeconds(1.8f);
            Assert.AreEqual(0, system.ActiveBombs);
            Assert.AreEqual(before - BlockRules.RedDamage(1), crystal.Health);
            Assert.AreEqual(1, system.Explosions);
        }

        [Test]
        public void TenHitsRollGreenAndCelesteAndClothingAloneChangesProbability()
        {
            system.PassivesEnabled = true;
            system.RandomValue = () => 1;
            for (int i = 0; i < 9; i++) system.NotifyMiningHit(Vector3.one * 1000, false);
            Assert.AreEqual(0, system.GreenRolls);
            Assert.AreEqual(0, system.CelesteRolls);
            system.NotifyMiningHit(Vector3.one * 1000, false);
            Assert.AreEqual(1, system.GreenRolls);
            Assert.AreEqual(1, system.CelesteRolls);
            Assert.AreEqual(0, system.HelperCount);
            float chance = BlockRules.Chance(root.Run.specialBlockClothingLevel);
            system.Upgrade(SpecialBlock.Green);
            Assert.AreEqual(8, BlockRules.HitInterval(system.Level(SpecialBlock.Green)));
            Assert.AreEqual(chance, BlockRules.Chance(root.Run.specialBlockClothingLevel));
            system.UpgradeClothing();
            Assert.Greater(BlockRules.Chance(root.Run.specialBlockClothingLevel), chance);
            var loaded = SaveStore.Deserialize(SaveStore.Serialize(root.Run));
            Assert.AreEqual(2, loaded.specialBlockLevels[(int)SpecialBlock.Green]);
            Assert.AreEqual(1, loaded.specialBlockClothingLevel);
        }

        [Test]
        public void UnselectedBluePassiveStillWorksAndWalkingDoesNotRoll()
        {
            system.PassivesEnabled = true;
            system.RandomValue = () => 0;
            system.Select((int)SpecialBlock.Red);
            var crystal = Crystal(CrystalColor.Blue, CrystalSize.Small);
            system.NotifyStep(crystal.transform.position);
            Assert.IsFalse(crystal.Nourished);
            Assert.IsTrue(system.TryNourishCrystal(crystal, true));
            Assert.IsTrue(crystal.Nourished);
        }

        [Test]
        public void SuccessfulPassivesAreBoundedAndHelpersDoLessRedDamage()
        {
            system.PassivesEnabled = true;
            system.RandomValue = () => 0;
            for (int i = 0; i < 10; i++) system.NotifyMiningHit(root.Player.transform.position, false);
            Assert.AreEqual(10, system.Explosions, "Red damage must not chain into more procs.");
            Assert.AreEqual(1, system.HelperCount);
            Assert.Greater(system.SpeedAt(root.Player.transform.position), 1);
            for (int i = 0; i < 30; i++) system.NotifyMiningHit(root.Player.transform.position, true);
            Assert.AreEqual(1, system.HelperCount, "Helpers cannot spawn helpers.");
            Assert.AreEqual(BlockRules.RedDamage(1) / 2, BlockRules.RedDamage(1, true));
            for (int i = 0; i < 8; i++) system.SpawnHelper(root.Player.transform.position);
            Assert.AreEqual(4, system.HelperCount);
        }

        [UnityTest]
        public IEnumerator HelpersPickUpFragmentsAndExpire()
        {
            var crystal = BreakableCrystal.All.First(c => c != null && c.Available && Vector3.Distance(c.transform.position, system.Lab.transform.position) < 15);
            root.Player.Teleport(system.Lab.transform.position + Vector3.back * 11);
            int before = root.Run.collectedCrystals;
            Vector3 spot = crystal.transform.position;
            crystal.TakeDamage(crystal.MaxHealth);
            yield return new WaitForSeconds(.8f); // the fragments scatter and settle
            system.SpawnHelper(spot + Vector3.right);
            yield return new WaitForSeconds(4);
            Assert.Greater(root.Run.collectedCrystals, before, "The helper did not pick up any fragment.");
            foreach (var helper in system.GetComponentsInChildren<BlockHelper>()) helper.expiresAt = Time.time;
            yield return null;
            yield return null;
            Assert.AreEqual(0, system.HelperCount);
        }

        [UnityTest]
        public IEnumerator ResetRestoresFifteenCrystalsAndShopUnlocksOnClose()
        {
            Crystal(CrystalColor.Yellow, CrystalSize.Small).TakeDamage(100);
            yield return new WaitForSeconds(.2f);
            system.OpenShop();
            Assert.IsTrue(Keys.Blocked);
            Assert.AreEqual(0f, Time.timeScale);
            system.ResetLab();
            system.CloseShop();
            yield return null;
            Assert.AreEqual(15, system.Lab.slots.Count(s => s.crystal != null && !s.crystal.Broken));
            Assert.IsFalse(Keys.Blocked);
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsTrue(Crystal(CrystalColor.Blue, CrystalSize.Small).Available);
        }
    }
}
