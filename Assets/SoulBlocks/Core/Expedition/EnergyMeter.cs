using UnityEngine;

namespace SoulBlocks
{
    // Pure drain: 1 unit of deltaTime per tick while active, paused while sheltered. Each
    // warning threshold (Balance.EnergyWarningHigh/Low) fires at most once per expedition;
    // ResetForExpedition clears that per-expedition memory.
    public sealed class EnergyMeter
    {
        public float Current { get; private set; }
        public float Max { get; private set; }
        public bool Depleted { get; private set; }
        bool warned25, warned10;

        public void ResetForExpedition(float max)
        {
            Max = max;
            Current = max;
            Depleted = max <= 0f;
            warned25 = false;
            warned10 = false;
        }

        // Keeps the meter in step with an externally owned value (Run.energy) without touching
        // the per-expedition warning memory.
        public void SyncFrom(float current, float max)
        {
            Max = max;
            Current = Mathf.Clamp(current, 0f, Mathf.Max(max, 0f));
            Depleted = Current <= 0f;
        }

        // Drains by deltaTime unless paused or already depleted. Returns the warning fraction
        // the first tick it is crossed, otherwise null.
        public float? Tick(float deltaTime, bool paused)
        {
            if (paused || Depleted || deltaTime <= 0f) return null;
            Current = Mathf.Max(0f, Current - deltaTime);
            float fraction = Max > 0f ? Current / Max : 0f;
            float? warning = null;
            if (!warned10 && fraction <= Balance.EnergyWarningLow) { warned10 = true; warning = Balance.EnergyWarningLow; }
            else if (!warned25 && fraction <= Balance.EnergyWarningHigh) { warned25 = true; warning = Balance.EnergyWarningHigh; }
            if (Current <= 0f) Depleted = true;
            return warning;
        }
    }
}
