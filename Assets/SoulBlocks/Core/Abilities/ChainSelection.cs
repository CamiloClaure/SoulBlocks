using System.Collections.Generic;
using UnityEngine;

namespace SoulBlocks
{
    // Golpe en cadena target picking: the nearest untouched target within range.
    public static class ChainSelection
    {
        // Index into targets of the nearest one to `from` within range whose index is not in
        // touchedIndices; -1 if none qualifies.
        public static int Next(Vector3 from, float range, IList<Vector3> targets, IList<int> touchedIndices)
        {
            int best = -1;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < targets.Count; i++)
            {
                if (touchedIndices.Contains(i)) continue;
                float distance = Vector3.Distance(from, targets[i]);
                if (distance <= range && distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }
            return best;
        }
    }
}
