using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SoulBlocks.Tests
{
    public class SculptableWorldTests
    {
        GameRoot root;
        TreeUpgradeAltar altar;
        [UnitySetUp] public IEnumerator Setup()
        {
            Time.timeScale = 1; Keys.Blocked = false;
            SceneManager.LoadScene("WorldBuilding");
            for (int i = 0; i < 15; i++) yield return null;
            root = GameRoot.Instance; root.Player.readInput = false;
            altar = TreeUpgradeAltar.Instance;
            SpecialBlockSystem.Instance.PassivesEnabled = false;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            SpecialBlockSystem.Instance.CloseShop(); root.GetComponent<ProgressionSystem>().Close();
            Time.timeScale = 1; Keys.Blocked = false;
            var empty = SceneManager.CreateScene("Sculptable cleanup"); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("WorldBuilding");
        }
        [UnityTest] public IEnumerator TerrainIsEditablePaintedAndSupportsWalkingOutsidePaths()
        {
            var terrain = Object.FindAnyObjectByType<Terrain>();
            Assert.IsNotNull(terrain); Assert.IsNotNull(terrain.GetComponent<TerrainCollider>());
            Assert.AreEqual(8, terrain.gameObject.layer);
            Assert.AreEqual(513, terrain.terrainData.heightmapResolution);
            Assert.AreEqual(3, terrain.terrainData.terrainLayers.Length);
            Assert.IsNotNull(root.Tree.GetComponent<CentralSpiritTree>());
            Assert.IsNull(Object.FindAnyObjectByType<WisdomTreeVisual>());
            Assert.AreEqual(new Vector3(48, 16, 48), terrain.terrainData.size);
            Assert.IsFalse(SoulPlayer.IsSupportedAt(new Vector3(35, 0, -35)), "Outside the island must remain void.");
            Assert.IsTrue(terrain.terrainData.IsHole(0, 0));
            Vector3 point = root.Tree.position + Vector3.left * 10;
            point.y = terrain.SampleHeight(point) + terrain.transform.position.y;
            Assert.IsTrue(SoulPlayer.IsSupportedAt(point));
            root.Player.Teleport(point + Vector3.up * .1f);
            yield return new WaitForSeconds(.2f);
            var before = root.Player.transform.position;
            for (int i = 0; i < 20; i++) { root.Player.StepMovement(Vector2.right, .02f); yield return null; }
            Assert.Greater(Vector3.Distance(before, root.Player.transform.position), 1);
        }
        [UnityTest] public IEnumerator AltarRaisesFiveCirclesBeforeOpeningAndHidesThemOnClose()
        {
            Assert.AreEqual(5, altar.symbols.Length);
            Assert.IsTrue(altar.symbols.All(s => !s.gameObject.activeSelf));
            Assert.IsFalse(altar.TryInteract(), "Must approach the altar first.");
            root.Player.Teleport(altar.transform.position + Vector3.back * 1.7f);
            Assert.IsTrue(altar.TryInteract());
            Assert.IsTrue(root.Player.MovementLocked);
            Assert.IsFalse(SpecialBlockSystem.Instance.ShopOpen);
            yield return new WaitForSecondsRealtime(.3f);
            Assert.Greater(altar.RevealProgress, 0); Assert.Less(altar.RevealProgress, 1);
            yield return new WaitForSecondsRealtime(.8f);
            Assert.IsTrue(SpecialBlockSystem.Instance.ShopOpen); Assert.AreEqual(0, Time.timeScale);
            Assert.IsTrue(altar.symbols.All(s => s.gameObject.activeSelf));
            foreach (var symbol in altar.symbols) Assert.Greater(symbol.position.y, altar.transform.position.y);
            Assert.IsTrue(SpecialBlockSystem.Instance.Upgrade(SpecialBlock.Yellow));
            Assert.AreEqual(2, SpecialBlockSystem.Instance.Level(SpecialBlock.Yellow));
            SpecialBlockSystem.Instance.CloseShop();
            yield return new WaitForSecondsRealtime(1);
            Assert.IsTrue(altar.symbols.All(s => !s.gameObject.activeSelf));
            Assert.IsFalse(Keys.Blocked); Assert.IsFalse(root.Player.MovementLocked);
        }
        [UnityTest] public IEnumerator OldTalentsNowUpgradeCentralTreeAndKeepAltarVisible()
        {
            root.Player.Teleport(altar.transform.position + Vector3.back * 1.7f);
            var progression = root.GetComponent<ProgressionSystem>();
            progression.OpenShop();
            yield return new WaitForSecondsRealtime(1);
            Assert.IsTrue(progression.IsOpen);
            Assert.IsTrue(altar.symbols.All(s => s.gameObject.activeSelf));
            root.Run.wisdom = 1000; root.Run.reserveSouls = 10000;
            root.Run.brokenByTier[0] = 1;
            Assert.IsTrue(progression.TryBuy("raiz1"));
            Assert.AreEqual(1, root.Tree.GetComponent<CentralSpiritTree>().Stage);
            Assert.AreEqual(1, root.Run.UpgradeLevel("raiz1"));
            progression.Close();
            yield return new WaitForSecondsRealtime(1);
            Assert.IsTrue(altar.symbols.All(s => !s.gameObject.activeSelf));
        }
        [UnityTest] public IEnumerator OldSpeedTalentSpendsSoulsAndIncreasesMovement()
        {
            root.Player.Teleport(altar.transform.position + Vector3.back * 1.7f);
            var progression = root.GetComponent<ProgressionSystem>();
            progression.OpenShop("velocidad");
            Assert.IsTrue(progression.IsOpen);
            root.Run.reserveSouls = 1000;
            Assert.IsFalse(progression.TryBuy("velocidad"), "The original Energy prerequisite remains.");
            Assert.IsTrue(progression.TryBuy("energia"));
            float before = root.Player.moveSpeed * root.Player.SpeedMultiplier;
            int souls = root.Run.reserveSouls;
            int cost = ProgressionRules.Cost(UpgradeCatalog.Get("velocidad"), 1);
            Assert.IsTrue(progression.TryBuy("velocidad"));
            Assert.AreEqual(souls - cost, root.Run.reserveSouls);
            Assert.AreEqual(before + Balance.VelocidadEffect, root.Player.moveSpeed * root.Player.SpeedMultiplier, .001f);
            progression.Close();
            yield return null;
        }
        [UnityTest] public IEnumerator HealthBarTracksDamageResetHiddenAndBrokenCrystals()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.position = root.Tree.position + Vector3.left * 10;
            var crystal = go.AddComponent<BreakableCrystal>();
            crystal.color = CrystalColor.Blue; crystal.ResetCrystal();
            Assert.IsTrue(crystal.ShowsHealthAt(go.transform.position));
            Assert.IsFalse(crystal.ShowsHealthAt(go.transform.position + Vector3.right * 30));
            Assert.AreEqual(1, crystal.HealthFraction);
            crystal.TakeDamage(30);
            Assert.AreEqual(120, crystal.Health); Assert.AreEqual(.8f, crystal.HealthFraction, .001f);
            crystal.startsHidden = true; crystal.ResetCrystal();
            Assert.IsFalse(crystal.ShowsHealthAt(go.transform.position));
            crystal.Reveal(); Assert.IsTrue(crystal.ShowsHealthAt(go.transform.position));
            Assert.AreEqual(1, crystal.HealthFraction);
            crystal.TakeDamage(crystal.MaxHealth);
            Assert.AreEqual(0, crystal.HealthFraction); Assert.IsFalse(crystal.ShowsHealthAt(go.transform.position));
            Object.Destroy(go);
            yield return null;
        }
        [UnityTest] public IEnumerator DisablingOpeningAltarReleasesPlayer()
        {
            root.Player.Teleport(altar.transform.position + Vector3.back * 1.7f);
            Assert.IsTrue(altar.TryInteract());
            yield return null;
            altar.gameObject.SetActive(false);
            Assert.IsFalse(Keys.Blocked); Assert.IsFalse(root.Player.MovementLocked);
            Assert.IsFalse(SpecialBlockSystem.Instance.ShopOpen);
        }
    }
}

