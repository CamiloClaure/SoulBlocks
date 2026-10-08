using System.Collections.Generic;
using UnityEngine;

namespace SoulBlocks
{
    public static class RaySegment
    {
        // Indices into targets, ordered by distance along from->to, whose sphere (XZ only;
        // Y is ignored, same as every other ground-plane check in this project) the segment
        // passes through. A target behind the clamp or off to the side with no overlap misses.
        public static List<int> Hits(Vector3 from, Vector3 to, IList<(Vector3 pos, float radius)> targets)
        {
            Vector2 a = new Vector2(from.x, from.z);
            Vector2 b = new Vector2(to.x, to.z);
            Vector2 ab = b - a;
            float lenSq = ab.sqrMagnitude;

            var ordered = new List<(int index, float t)>();
            for (int i = 0; i < targets.Count; i++)
            {
                Vector2 p = new Vector2(targets[i].pos.x, targets[i].pos.z);
                float t = lenSq > .0001f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / lenSq) : 0f;
                Vector2 closest = a + ab * t;
                if (Vector2.Distance(p, closest) <= targets[i].radius)
                    ordered.Add((i, t));
            }
            ordered.Sort((x, y) => x.t.CompareTo(y.t));

            var result = new List<int>(ordered.Count);
            foreach (var hit in ordered) result.Add(hit.index);
            return result;
        }
    }
}
