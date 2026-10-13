using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SoulBlocks.Tests
{
    public class PickAbilityPlayModeTests
    {
        sealed class FakeCrystal : IMineable
        {
            public int PrisonId { get; }
            public Vector3 Position { get; }
            public float Radius => 1f;
            public int HitCount;

            public FakeCrystal(int id, Vector3 position) { PrisonId = id; Position = position; }

            public PrisonForecast Forecast(int pickLevel) => new PrisonForecast { Kind = ForecastKind.Hits, hits = 1 };

            public int ApplyPickHit(int pickLevel) { HitCount++; return 1; }
        }

        [UnityTest]
        public IEnumerator OneBlowPerPickIntervalAndThePlayerFacesTheCrystal()
        {
            SceneManager.LoadScene("SoulBlocks");
            for (int i = 0; i < 10; i++) yield return null;

            var root = GameRoot.Instance;
            var player = root.Player;
            player.readInput = false;
            var pick = root.GetComponent<PickAbility>();
            Assert.IsNotNull(pick, "PickAbility was not attached.");

            var crystal = new FakeCrystal(1, player.transform.position + Vector3.right * 2f);
            MineableRegistry.All.Add(crystal);
            try
            {
                Assert.IsTrue(pick.TryHit(crystal), "The first blow should land.");
                Assert.IsFalse(pick.TryHit(crystal), "A second blow right away should wait for PickInterval.");
                Assert.AreEqual(1, crystal.HitCount);
                Assert.AreEqual(90f, player.Yaw, .01f, "The player should turn toward the crystal (+x).");

                yield return new WaitForSeconds(root.Stats.Get(StatId.PickInterval) + .1f); // time, not frames: batchmode frames are ~1 ms
                Assert.IsTrue(pick.TryHit(crystal), "After PickInterval the pick is ready again.");
                Assert.AreEqual(2, crystal.HitCount);
            }
            finally
            {
                MineableRegistry.All.Remove(crystal);
            }
        }
    }
}
