using System.Collections.Generic;
using UnityEngine;

namespace SoulBlocks
{
    // Something the pick can chip at: today only crystal prisons (PrisonView).
    public interface IMineable
    {
        int PrisonId { get; }
        Vector3 Position { get; }
        float Radius { get; }
        PrisonForecast Forecast(int pickLevel);

        // One pick blow. Returns the damage actually dealt.
        int ApplyPickHit(int pickLevel);
    }

    public static class MineableRegistry
    {
        public static readonly List<IMineable> All = new List<IMineable>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Clear() => All.Clear();
    }
}
