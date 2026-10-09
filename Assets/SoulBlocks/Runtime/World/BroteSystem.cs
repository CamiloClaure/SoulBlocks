using UnityEngine;

namespace SoulBlocks
{
    // Wisdom's "Brote" (section 1.7): once unlocked, souls trickle straight into the reserve
    // near the tree without the player collecting them, plus a placeholder sprout cluster that
    // grows a little with every tree stage. No model/animation, just primitives per obra gruesa.
    public sealed class BroteSystem : MonoBehaviour
    {
        readonly BroteTicker ticker = new BroteTicker();
        GameObject sproutRoot;
        int builtStage = -1;
        float lastToastTime = -999f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() => GameRoot.Register<BroteSystem>();

        void OnEnable()
        {
            GameEvents.MapBuilt += OnMapBuilt;
            GameEvents.TreeStageChanged += OnTreeStageChanged;
        }

        void OnDisable()
        {
            GameEvents.MapBuilt -= OnMapBuilt;
            GameEvents.TreeStageChanged -= OnTreeStageChanged;
        }

        void OnMapBuilt(MapLayout layout)
        {
            var run = GameRoot.Instance != null ? GameRoot.Instance.Run : null;
            RebuildSprouts(run != null ? run.treeStage : 0);
        }

        void OnTreeStageChanged(int stage) => RebuildSprouts(stage);

        void Update()
        {
            var root = GameRoot.Instance;
            if (root == null || root.Run == null) return;
            var run = root.Run;
            if (!run.HasUnlock(UnlockIds.Brote)) return;

            int ticks = ticker.Tick(Time.deltaTime, Balance.BroteIntervalSeconds);
            if (ticks <= 0) return;

            int amount = ticks * Balance.BroteSoulsPerTick;
            run.reserveSouls += amount;
            GameEvents.RaiseSoulsDeposited(amount);

            if (Time.time - lastToastTime < Balance.BroteToastCooldownSeconds) return;
            lastToastTime = Time.time;
            Hud.Toast("Brote: +" + amount + " almas");
        }

        void RebuildSprouts(int stage)
        {
            stage = Mathf.Clamp(stage, 0, 4);
            if (stage == builtStage) return;
            builtStage = stage;

            var root = GameRoot.Instance;
            if (root == null || root.Tree == null) return;
            if (sproutRoot != null) Destroy(sproutRoot);

            sproutRoot = new GameObject("Brote Sprouts");
            sproutRoot.transform.SetParent(root.Tree, false);

            int count = BroteTicker.SproutCountForStage(stage);
            // ponytail: position/count only need to look alive and vary with stage, not be part
            // of the seeded run, same call WisdomTreeVisual already makes for its lantern spread.
            var rng = new System.Random(20261008);
            for (int i = 0; i < count; i++)
            {
                float angle = i * Mathf.PI * 2f / count;
                float radius = 1.2f + (float)rng.NextDouble() * .6f;
                Vector3 local = new Vector3(Mathf.Cos(angle) * radius, .1f, Mathf.Sin(angle) * radius);
                Placeholder.Make(PrimitiveType.Sphere, new Color(.4f, 1f, .5f), "Sprout",
                    sproutRoot.transform, root.Tree.position + local, Vector3.one * .25f, false, .6f);
            }
        }
    }
}
