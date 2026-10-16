using UnityEngine;
using UnityEngine.SceneManagement;

namespace SoulBlocks
{
    // Keeps its children hidden until the run owns `unlock` (a UnlockIds value): gated paths,
    // platforms and the sites placed on them. SceneLayout reads the nearest gate above a site
    // as that site's requiresUnlock, so prisons and tiles behind a gate stay locked in the
    // MapLayout too. Edit mode never hides anything, so gated content stays editable.
    public sealed class UnlockGate : MonoBehaviour
    {
        public string unlock = UnlockIds.Raiz3;

        void OnEnable()
        {
            GameEvents.UnlockGranted += OnUnlockGranted;
            // A gate nested under another one wakes up when the outer one opens.
            if (GameRoot.Instance != null && GameRoot.Instance.Run != null) Apply(GameRoot.Instance.Run);
        }

        void OnDisable() => GameEvents.UnlockGranted -= OnUnlockGranted;

        void OnUnlockGranted(string id)
        {
            if (id == unlock) SetOpen(true);
        }

        public void Apply(RunState run) => SetOpen(run != null && run.HasUnlock(unlock));

        void SetOpen(bool open)
        {
            foreach (Transform child in transform) child.gameObject.SetActive(open);
        }

        // GameRoot calls this before MapBuilt, so nothing populates a path that is still locked.
        public static void ApplyAll(Scene scene, RunState run)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var gate in root.GetComponentsInChildren<UnlockGate>(true))
                    gate.Apply(run);
        }

        // The unlock of the nearest gate above (or on) `target`, "" when nothing gates it.
        public static string RequiredFor(Transform target)
        {
            for (var t = target; t != null; t = t.parent)
            {
                var gate = t.GetComponent<UnlockGate>();
                if (gate != null) return gate.unlock;
            }
            return "";
        }
    }
}
