using System.Collections.Generic;
using UnityEngine;

namespace SoulBlocks
{
    public interface IRayTarget
    {
        int PrisonId { get; }
        Vector3 Position { get; }
        float Radius { get; }
        PrisonForecast Forecast(int rayLevel);

        // Returns the damage actually dealt.
        int ApplyRayHit(int rayLevel);
    }

    public static class RayTargetRegistry
    {
        public static readonly List<IRayTarget> All = new List<IRayTarget>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Clear() => All.Clear();
    }
}
