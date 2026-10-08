using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SoulBlocks.Tests
{
    public class RayhitAbilityPlayModeTests
    {
        sealed class FakeRayTarget : IRayTarget
        {
            public int PrisonId { get; }
            public Vector3 Position { get; }
            public float Radius => 1f;
            public int HitCount;

            public FakeRayTarget(int id, Vector3 position) { PrisonId = id; Position = position; }

            public PrisonForecast Forecast(int rayLevel) => new PrisonForecast { Kind = ForecastKind.Hits, hits = 1 };

            public int ApplyRayHit(int rayLevel) { HitCount++; return 1; }
        }

        [UnityTest]
        public IEnumerator FireThroughTwoTargetsHitsBothAndReportsTwo()
        {
            SceneManager.LoadScene("SoulBlocks");
            for (int i = 0; i < 10; i++) yield return null;

            var root = GameRoot.Instance;
            var player = root.Player;
            player.readInput = false;
            var rayhit = root.GetComponent<RayhitAbility>();
            Assert.IsNotNull(rayhit, "RayhitAbility was not attached.");

            var branch = root.Layout.branches[0];
            Vector3 start = branch.anchor + branch.direction * 1f;
            player.Teleport(start);
            for (int i = 0; i < 3; i++) yield return null;

            var near = new FakeRayTarget(1, start + branch.direction * 2f);
            var far = new FakeRayTarget(2, start + branch.direction * 4f);
            RayTargetRegistry.All.Add(near);
            RayTargetRegistry.All.Add(far);

            int reportedHits = -1;
            GameEvents.RayhitFired += (from, to, hits) => reportedHits = hits;

            try
            {
                Assert.IsTrue(rayhit.Fire(start + branch.direction * 8f), "Fire should consume a charge and start travel.");
                for (int i = 0; i < 30; i++) yield return null;

                Assert.AreEqual(1, near.HitCount, "Near target was not hit.");
                Assert.AreEqual(1, far.HitCount, "Far target was not hit.");
                Assert.AreEqual(2, reportedHits, "RayhitFired did not report 2 hits.");
            }
            finally
            {
                RayTargetRegistry.All.Remove(near);
                RayTargetRegistry.All.Remove(far);
            }
        }
    }
}
