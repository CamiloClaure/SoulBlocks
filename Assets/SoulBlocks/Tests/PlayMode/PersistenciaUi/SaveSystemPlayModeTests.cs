using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SoulBlocks.Tests
{
    public class SaveSystemPlayModeTests
    {
        string tempPath;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SaveSystem.PathOverride = null;
            if (tempPath != null && File.Exists(tempPath)) File.Delete(tempPath);
            string bak = tempPath + ".bak";
            if (tempPath != null && File.Exists(bak)) File.Delete(bak);
            tempPath = null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator SaveNowThenTryLoadRoundTripsModifiedRun()
        {
            bool mapBuilt = false;
            GameEvents.MapBuilt += _ => mapBuilt = true;

            SceneManager.LoadScene("SoulBlocks");
            for (int i = 0; i < 10; i++) yield return null;
            Assert.IsTrue(mapBuilt, "MapBuilt was not raised.");

            tempPath = Path.Combine(Application.temporaryCachePath, "persistencia-ui-test-save.json");
            if (File.Exists(tempPath)) File.Delete(tempPath);
            SaveSystem.PathOverride = tempPath;

            var run = GameRoot.Instance.Run;
            run.wisdom = 42;
            run.AddUnlock(UnlockIds.Raiz1);

            SaveSystem.SaveNow();
            Assert.IsTrue(File.Exists(tempPath), "SaveNow did not write a file.");

            run.wisdom = 0; // mutate the live Run so the comparison can only pass from disk
            var loaded = SaveSystem.TryLoad();

            Assert.IsNotNull(loaded, "TryLoad returned null for a save that was just written.");
            Assert.AreEqual(42, loaded.wisdom);
            Assert.IsTrue(loaded.HasUnlock(UnlockIds.Raiz1));
            Assert.IsTrue(SaveSystem.HasValidSave());
        }

        [UnityTest]
        public IEnumerator CorruptSaveIsMovedAsideAndLoadReturnsNull()
        {
            bool mapBuilt = false;
            GameEvents.MapBuilt += _ => mapBuilt = true;

            SceneManager.LoadScene("SoulBlocks");
            for (int i = 0; i < 10; i++) yield return null;
            Assert.IsTrue(mapBuilt, "MapBuilt was not raised.");

            tempPath = Path.Combine(Application.temporaryCachePath, "persistencia-ui-test-corrupt.json");
            File.WriteAllText(tempPath, "{ not actually json");
            SaveSystem.PathOverride = tempPath;

            Assert.IsFalse(SaveSystem.HasValidSave());
            var loaded = SaveSystem.TryLoad();

            Assert.IsNull(loaded);
            Assert.IsFalse(File.Exists(tempPath), "Corrupt file should have been moved aside.");
            Assert.IsTrue(File.Exists(tempPath + ".bak"), "Corrupt file should have been renamed to .bak.");
        }
    }
}
