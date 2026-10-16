using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SoulBlocks.Tests
{
    public class SpiritWorldExpansionTests
    {
        SoulPlayer player;
        GuidingTorch torch;
        [UnitySetUp] public IEnumerator Setup()
        {
            Keys.Blocked = false; Time.timeScale = 1;
            SceneManager.LoadScene(GameRoot.SceneName);
            for (int i = 0; i < 15; i++) yield return null; StartMenu.Dismiss();
            player = GameRoot.Instance.Player; player.readInput = false;
            torch = Object.FindAnyObjectByType<GuidingTorch>();
            SpecialBlockSystem.Instance.PassivesEnabled = false;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale = 1; Keys.Blocked = false;
            var empty = SceneManager.CreateScene("Expansion cleanup"); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync(GameRoot.SceneName);
        }
        [UnityTest] public IEnumerator DenseSpiritsStayOutsideEveryAuthoredRoad()
        {
            var population = Object.FindAnyObjectByType<PathPopulation>();
            Assert.Greater(population.SpiritCount, 100);
            Assert.IsTrue(Object.FindObjectsByType<WanderingSpirit>().Any(s => s.name.StartsWith("Void Soul") && !population.InsideRoad(s.transform.position, 10) && !SoulPlayer.IsSupportedAt(s.transform.position)));
            yield return new WaitForSeconds(.5f);
            foreach (var spirit in Object.FindObjectsByType<WanderingSpirit>())
            {
                Assert.IsFalse(population.InsideRoad(spirit.transform.position, 0));
                Assert.IsNotNull(spirit.GetComponentInChildren<SpiritGlow>());
                Assert.IsNull(spirit.GetComponentInChildren<TrailRenderer>());
                var tint = spirit.visual.GetComponentInChildren<MeshRenderer>().sharedMaterial.color;
                Assert.Greater(tint.r, .9f); Assert.Greater(tint.g, .7f); Assert.Less(tint.b, .2f);
            }
        }
        [UnityTest] public IEnumerator GuidedSpiritsStayBehindPlayerAfterTurningAndPauseWithMenus()
        {
            player.Teleport(torch.AltarPosition + Vector3.right * 1.7f);
            Assert.IsTrue(torch.Interact(player));
            yield return null;
            GameRoot.Instance.Stats.SetModifier("test", StatId.SpiritCapacity, 1000f); // following, not the Carga cap, is under test
            var spirits = Object.FindObjectsByType<WanderingSpirit>().Take(16).ToArray();
            for (int i = 0; i < spirits.Length; i++)
            {
                spirits[i].phase = i * 2.39996f;
                spirits[i].transform.position = player.transform.position + Vector3.up + Vector3.right * .3f;
            }
            yield return new WaitForSeconds(1.2f);
            Assert.IsTrue(spirits.All(s => s.FollowingBehind));
            var forward = Quaternion.Euler(0, player.Yaw, 0) * Vector3.forward;
            foreach (var spirit in spirits)
                Assert.Less(Vector3.Dot(spirit.transform.position - player.transform.position, forward), -1.5f);
            player.FaceDirection(-forward);
            yield return new WaitForSeconds(1.2f);
            foreach (var spirit in spirits)
                Assert.Less(Vector3.Dot(spirit.transform.position - player.transform.position, -forward), -1.5f);
            var before = spirits[0].transform.position;
            Keys.Blocked = true; yield return new WaitForSeconds(.15f);
            Assert.AreEqual(before, spirits[0].transform.position);
            Keys.Blocked = false;
            torch.SetLit(false); yield return null;
            Assert.IsTrue(spirits.All(s => !s.FollowingBehind && !s.Following));
        }
        [UnityTest] public IEnumerator NewRoadReachesArenaAndHubHasFiveUpgrades()
        {
            var road = GameObject.Find("Power Test Path").GetComponent<AuthoredPath>();
            Assert.Greater(Vector3.Distance(road.transform.TransformPoint(road.Evaluate(0)), road.transform.TransformPoint(road.Evaluate(1))), 60);
            for (int i = 0; i <= 100; i++)
            {
                var p = road.transform.TransformPoint(road.Evaluate(i / 100f));
                Assert.IsTrue(SoulPlayer.IsSupportedAt(p + Vector3.up * .05f), "Road must be walkable at " + i);
            }
            var arena = Object.FindAnyObjectByType<PowerupTestArena>();
            Assert.AreEqual(72, arena.slots.Length);
            Assert.AreEqual(5, arena.slots.Select(s => s.color).Distinct().Count());
            Assert.AreEqual(3, arena.slots.Select(s => s.size).Distinct().Count());
            var station = Object.FindAnyObjectByType<TreeUpgradeAltar>();
            Assert.Less(Vector3.Distance(station.transform.position, GameRoot.Instance.Tree.position), 9);
            Assert.AreEqual(5, station.symbols.Length);
            foreach (var tree in NourishableTree.All)
            {
                var size = tree.GetComponent<BoxCollider>().size;
                Assert.Greater(size.y, 4); Assert.Less(size.z, .5f);
            }
            arena.slots[0].crystal.TakeDamage(1000);
            yield return new WaitForSeconds(.2f);
            SpecialBlockSystem.Instance.ResetLab();
            yield return null;
            Assert.AreEqual(72, arena.slots.Count(s => s.crystal != null && s.crystal.Available));
        }
    }
}

