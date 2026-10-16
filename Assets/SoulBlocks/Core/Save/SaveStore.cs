namespace SoulBlocks
{
    // Pure (de)serialization for RunState: no file I/O here, that is Runtime/Save/SaveSystem.cs.
    // Kept separate so EditMode tests can round-trip a save without touching disk.
    public static class SaveStore
    {
        // 2: hand-built world (no map seed, no cargo or ground souls).
        public const int CurrentVersion = 2;

        public static string Serialize(RunState run) => UnityEngine.JsonUtility.ToJson(run);

        // Null on empty/malformed JSON or a save version this build does not understand; never
        // throws. The caller (SaveSystem) decides what to do with a null result (log, .bak it).
        public static RunState Deserialize(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                var run = UnityEngine.JsonUtility.FromJson<RunState>(json);
                if (run == null || run.version != CurrentVersion) return null;
                return run;
            }
            catch
            {
                return null;
            }
        }
    }
}
