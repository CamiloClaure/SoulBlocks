using System;
using System.IO;
using UnityEngine;

namespace SoulBlocks
{
    // Writes Run to disk on the moments that matter (expedition end, purchase, victory, quit)
    // and on demand (pause menu's "Guardar", start menu's "Continuar" reads it back). Atomic
    // write: temp file then delete-and-move over the real one, so a crash mid-write never
    // corrupts the save that is already on disk. Load never throws to the player: missing,
    // corrupt or version-mismatched all come back null; a corrupt file is renamed to .bak
    // instead of lost.
    public sealed class SaveSystem : MonoBehaviour
    {
        // Tests point this at a temp file instead of the real persistentDataPath.
        public static string PathOverride;

        static string SavePath => PathOverride ?? Path.Combine(Application.persistentDataPath, "soulblocks-save.json");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() => GameRoot.Register<SaveSystem>();

        void OnEnable()
        {
            GameEvents.ExpeditionEnded += OnExpeditionEnded;
            GameEvents.UpgradePurchased += OnUpgradePurchased;
            GameEvents.GameWon += OnGameWon;
        }

        void OnDisable()
        {
            GameEvents.ExpeditionEnded -= OnExpeditionEnded;
            GameEvents.UpgradePurchased -= OnUpgradePurchased;
            GameEvents.GameWon -= OnGameWon;
        }

        void OnExpeditionEnded(ExpeditionEndReason reason) => SaveNow();
        void OnUpgradePurchased(string id, int level) => SaveNow();
        void OnGameWon() => SaveNow();
        void OnApplicationQuit() => SaveNow();

        // Public for the pause menu and for bots/tests: save the current Run right now.
        public static void SaveNow()
        {
            var root = GameRoot.Instance;
            if (root == null || root.Run == null) return;
            WriteAtomic(SavePath, SaveStore.Serialize(root.Run));
        }

        static void WriteAtomic(string path, string json)
        {
            try
            {
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                string temp = path + ".tmp";
                File.WriteAllText(temp, json);
                if (File.Exists(path)) File.Delete(path);
                File.Move(temp, path);
            }
            catch (Exception e)
            {
                Debug.LogWarning("SaveSystem: write failed: " + e.Message);
            }
        }

        // Null on missing/corrupt/version-mismatch. A corrupt or wrong-version file is moved
        // aside as "<path>.bak" (never deleted) so a bad write never silently loses the save.
        public static RunState TryLoad()
        {
            string path = SavePath;
            if (!File.Exists(path)) return null;

            string json;
            try { json = File.ReadAllText(path); }
            catch (Exception e) { Debug.LogWarning("SaveSystem: read failed: " + e.Message); return null; }

            var run = SaveStore.Deserialize(json);
            if (run != null) return run;

            Debug.LogWarning("SaveSystem: save file corrupt or unreadable version, moving aside as .bak");
            try
            {
                string bak = path + ".bak";
                if (File.Exists(bak)) File.Delete(bak);
                File.Move(path, bak);
            }
            catch (Exception e)
            {
                Debug.LogWarning("SaveSystem: could not move corrupt save aside: " + e.Message);
            }
            return null;
        }

        // Read-only check for the start menu's "Continuar" button: does a loadable save exist,
        // without the side effect of renaming a corrupt one aside.
        public static bool HasValidSave()
        {
            string path = SavePath;
            string json;
            try { json = File.Exists(path) ? File.ReadAllText(path) : null; }
            catch { return false; }
            return SaveStore.Deserialize(json) != null;
        }
    }
}
