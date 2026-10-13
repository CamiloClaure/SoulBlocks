using UnityEngine;

namespace SoulBlocks
{
    // Pure charge counter (Dash): an integer Current, clamped to a max that
    // the owner re-supplies every frame (stats can change mid-run via upgrades), filled by two
    // independent progress tracks that never overflow into each other: elapsed time (Tick) and
    // an external push (AddProgress, souls collected). Magnet does not use this: it is a plain
    // cooldown, not a charge count.
    public sealed class ChargeMeter
    {
        public int Current { get; private set; }

        int max;
        float rechargeSeconds;
        float soulsPerCharge = -1f; // <= 0: soul-based recharge disabled
        float timeProgress;
        float soulProgress;

        public ChargeMeter(int max, float rechargeSeconds, int startCharges = -1)
        {
            this.max = Mathf.Max(0, max);
            this.rechargeSeconds = Mathf.Max(.0001f, rechargeSeconds);
            Current = Mathf.Clamp(startCharges < 0 ? this.max : startCharges, 0, this.max);
        }

        // Called every frame by the owner with the live stat values; clamps Current down if max shrank.
        public void Configure(int max, float rechargeSeconds, float soulsPerCharge = -1f)
        {
            this.max = Mathf.Max(0, max);
            this.rechargeSeconds = Mathf.Max(.0001f, rechargeSeconds);
            this.soulsPerCharge = soulsPerCharge;
            if (Current > this.max) Current = this.max;
        }

        public void Tick(float deltaTime)
        {
            if (Current >= max) { timeProgress = 0f; return; }
            timeProgress += deltaTime;
            Gain(ref timeProgress, rechargeSeconds);
        }

        // Soul-based recharge: the caller pushes however many souls were just collected.
        public void AddProgress(float souls)
        {
            if (soulsPerCharge <= 0f || Current >= max) return;
            soulProgress += souls;
            Gain(ref soulProgress, soulsPerCharge);
        }

        public bool TryConsume()
        {
            if (Current <= 0) return false;
            Current--;
            return true;
        }

        void Gain(ref float progress, float threshold)
        {
            while (progress >= threshold && Current < max)
            {
                progress -= threshold;
                Current++;
            }
            if (Current >= max) progress = 0f;
        }
    }
}
