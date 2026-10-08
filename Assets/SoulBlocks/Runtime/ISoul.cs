using System.Collections.Generic;
using UnityEngine;

namespace SoulBlocks
{
    public interface ISoul
    {
        Vector3 Position { get; }
        int Value { get; }
        bool Available { get; }
        void MoveTo(Vector3 position);
        void Collect();
    }

    public static class SoulRegistry
    {
        public static readonly List<ISoul> All = new List<ISoul>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Clear() => All.Clear();
    }
}
