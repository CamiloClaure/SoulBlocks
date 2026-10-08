using System.Collections.Generic;
using UnityEngine;

namespace SoulBlocks
{
    public static class MagnetSelection
    {
        // Indices into souls, nearest-first, greedily taking only the ones whose value still
        // fits what is left of freeCapacity. A soul that would overflow is skipped (not a stop):
        // a smaller one farther away can still fit.
        public static List<int> Select(Vector3 from, float radius, int freeCapacity, IList<(Vector3 pos, int value)> souls)
        {
            var candidates = new List<(int index, float distance)>();
            for (int i = 0; i < souls.Count; i++)
            {
                float distance = Vector3.Distance(from, souls[i].pos);
                if (distance <= radius) candidates.Add((i, distance));
            }
            candidates.Sort((x, y) => x.distance.CompareTo(y.distance));

            var result = new List<int>();
            int used = 0;
            foreach (var candidate in candidates)
            {
                int value = souls[candidate.index].value;
                if (used + value > freeCapacity) continue;
                used += value;
                result.Add(candidate.index);
            }
            return result;
        }
    }
}
