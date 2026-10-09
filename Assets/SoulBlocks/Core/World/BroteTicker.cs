using UnityEngine;

namespace SoulBlocks
{
    // Pure accumulator for the Brote automation (section 1.7): how many whole
    // Balance.BroteIntervalSeconds ticks a deltaTime step produced, carrying the remainder.
    // Not persisted in RunState: a loaded save just starts counting again, losing at most one
    // partial interval, which is not worth a save field for a passive trickle.
    public sealed class BroteTicker
    {
        float accumulator;

        public int Tick(float deltaTime, float intervalSeconds)
        {
            if (intervalSeconds <= 0f || deltaTime <= 0f) return 0;
            accumulator += deltaTime;
            int ticks = 0;
            while (accumulator >= intervalSeconds)
            {
                accumulator -= intervalSeconds;
                ticks++;
            }
            return ticks;
        }

        // How many placeholder sprouts to show near the tree at this stage (0..4): grows by one
        // per stage off a small base, clamped so an out-of-range stage never throws.
        public static int SproutCountForStage(int treeStage) => Balance.BroteSproutsBase + Mathf.Clamp(treeStage, 0, 4);
    }
}
