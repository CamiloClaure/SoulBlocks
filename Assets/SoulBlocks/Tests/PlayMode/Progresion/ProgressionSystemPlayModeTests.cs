using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SoulBlocks.Tests
{
    public class ProgressionSystemPlayModeTests
    {
        [UnityTest]
        public IEnumerator BuyingCargaRaisesCargoCapacityAndStatsChanged()
        {
            bool mapBuilt = false;
            GameEvents.MapBuilt += _ => mapBuilt = true;

            SceneManager.LoadScene("SoulBlocks");
            for (int i = 0; i < 10; i++) yield return null;
            Assert.IsTrue(mapBuilt, "MapBuilt was not raised.");

            var root = GameRoot.Instance;
            Assert.IsNotNull(root, "GameRoot did not boot.");
            var progression = root.GetComponent<ProgressionSystem>();
            Assert.IsNotNull(progression, "ProgressionSystem was not attached.");

            root.Run.reserveSouls = 1000;

            bool statsChanged = false;
            GameEvents.StatsChanged += () => statsChanged = true;

            Assert.IsTrue(progression.TryBuy("carga"));
            yield return null;

            Assert.AreEqual(150f, root.Stats.Get(StatId.CargoCapacity), .0001f);
            Assert.IsTrue(statsChanged, "StatsChanged was not raised.");
        }
    }
}
