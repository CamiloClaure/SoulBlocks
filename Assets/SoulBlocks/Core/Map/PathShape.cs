using System.Collections.Generic;
using UnityEngine;

namespace SoulBlocks
{
    // The visible ground of the paths: a quadratic B-spline ribbon through the (jittered) tile
    // centers, with a half width per tile. Tile i owns exactly one Bezier piece: from the
    // midpoint with its previous neighbor, control point at its own center, to the midpoint
    // with its next neighbor. Neighboring pieces meet at that midpoint with the same tangent,
    // so the curve is smooth and never has a corner; the 10 m grid stays the topology
    // (occupancy, A*, validator), this only decides the shape. Locking tiles one by one (deep
    // segments, bridges) still works because each tile has its own piece.
    public static class PathShape
    {
        // Fills owners[i].edgeLeft/edgeRight for every non-null owner; owned points never sit at
        // either end of the chain (callers add a pseudo or foreign point there). With
        // extendToForeignEnds, an owned tile next to an unowned end also covers the straight
        // run from that end point to its midpoint (bridges reaching into a branch end).
        public static void BuildPieces(List<Vector3> points, List<float> halfWidths, List<TileLayout> owners, bool extendToForeignEnds = false)
        {
            int m = points.Count - 1;
            for (int i = 1; i < m; i++)
            {
                var tile = owners[i];
                if (tile == null) continue;
                tile.edgeLeft.Clear();
                tile.edgeRight.Clear();
                int samples = Mathf.Max(2, Balance.PathSamplesPerSegment);

                if (extendToForeignEnds && owners[i - 1] == null)
                    for (int k = 0; k < samples / 2; k++) Straight(points, halfWidths, i - 1, i, k / (float)samples, tile);
                for (int k = 0; k <= samples; k++)
                {
                    Piece(points, halfWidths, i, k / (float)samples, out var left, out var right);
                    tile.edgeLeft.Add(left);
                    tile.edgeRight.Add(right);
                }
                if (extendToForeignEnds && owners[i + 1] == null)
                    for (int k = 1; k <= samples / 2; k++) Straight(points, halfWidths, i, i + 1, .5f + k / (float)samples, tile);
            }
        }

        // Straight run along points[a]->points[b] at fraction f (no curvature, no width cap).
        static void Straight(List<Vector3> points, List<float> halfWidths, int a, int b, float f, TileLayout tile)
        {
            Vector3 direction = points[b] - points[a];
            direction.y = 0f;
            Vector3 normal = new Vector3(-direction.z, 0f, direction.x).normalized;
            Vector3 center = Vector3.Lerp(points[a], points[b], f);
            float half = Mathf.Lerp(halfWidths[a], halfWidths[b], f);
            tile.edgeLeft.Add(center + normal * half);
            tile.edgeRight.Add(center - normal * half);
        }

        // Tile i's Bezier at t. The inner side of a bend is capped at .9 of the tightest turn
        // radius anywhere on the piece, so the ribbon never folds over itself even where the
        // curvature peaks between samples. At the shared ends (t = 0 or 1) the cap is the
        // smaller of both pieces' caps, so both compute the same boundary edge.
        static void Piece(List<Vector3> points, List<float> halfWidths, int i, float t, out Vector3 left, out Vector3 right)
        {
            Bezier(points, i, t, out var center, out var velocity, out _);
            PieceCaps(points, i, out float leftCap, out float rightCap);
            if (t <= 0f && i - 1 >= 1) { PieceCaps(points, i - 1, out float l, out float r); leftCap = Mathf.Min(leftCap, l); rightCap = Mathf.Min(rightCap, r); }
            if (t >= 1f && i + 1 <= points.Count - 2) { PieceCaps(points, i + 1, out float l, out float r); leftCap = Mathf.Min(leftCap, l); rightCap = Mathf.Min(rightCap, r); }

            float w0 = (halfWidths[i - 1] + halfWidths[i]) * .5f, w1 = (halfWidths[i] + halfWidths[i + 1]) * .5f;
            float u = 1f - t;
            float half = u * u * w0 + 2f * u * t * halfWidths[i] + t * t * w1;

            float speed = Mathf.Sqrt(velocity.x * velocity.x + velocity.z * velocity.z);
            Vector3 normal = speed > 1e-4f ? new Vector3(-velocity.z / speed, 0f, velocity.x / speed) : Vector3.right;
            left = center + normal * Mathf.Min(half, leftCap);
            right = center - normal * Mathf.Min(half, rightCap);
        }

        // Positive curvature turns toward the left normal, so it caps the left (inner) side.
        // ponytail: 32 samples per piece is plenty for a 10 m quadratic; solve for the curvature
        // peak analytically if this ever shows up in a profile.
        static void PieceCaps(List<Vector3> points, int i, out float leftCap, out float rightCap)
        {
            leftCap = rightCap = float.MaxValue;
            for (int k = 0; k <= 32; k++)
            {
                Bezier(points, i, k / 32f, out _, out _, out float curvature);
                if (curvature > 1e-4f) leftCap = Mathf.Min(leftCap, .9f / curvature);
                else if (curvature < -1e-4f) rightCap = Mathf.Min(rightCap, .9f / -curvature);
            }
        }

        static void Bezier(List<Vector3> points, int i, float t, out Vector3 center, out Vector3 velocity, out float curvature)
        {
            Vector3 control = points[i];
            Vector3 from = (points[i - 1] + control) * .5f, to = (control + points[i + 1]) * .5f;
            float u = 1f - t;
            center = u * u * from + 2f * u * t * control + t * t * to;
            velocity = 2f * u * (control - from) + 2f * t * (to - control);
            Vector3 acceleration = 2f * (to - 2f * control + from);
            float speed = Mathf.Sqrt(velocity.x * velocity.x + velocity.z * velocity.z);
            curvature = speed > 1e-4f ? (velocity.x * acceleration.z - velocity.z * acceleration.x) / (speed * speed * speed) : 0f;
        }

        // A point inside the tile's ribbon (or its platform disc), at least `margin` meters from
        // either edge where the ribbon is wide enough for that.
        public static Vector3 RandomPoint(TileLayout tile, SeededRng rng, float margin)
        {
            if (tile.shortcut || tile.edgeLeft.Count < 2)
            {
                float radius = Mathf.Max(0f, Balance.PlatformSize * .5f - margin) * Mathf.Sqrt(rng.NextFloat());
                float angle = rng.NextFloat() * Mathf.PI * 2f;
                return tile.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            }
            int i = rng.Next(0, tile.edgeLeft.Count - 1);
            float along = rng.NextFloat();
            Vector3 left = Vector3.Lerp(tile.edgeLeft[i], tile.edgeLeft[i + 1], along);
            Vector3 right = Vector3.Lerp(tile.edgeRight[i], tile.edgeRight[i + 1], along);
            return Across(left, right, margin, rng);
        }

        // Left/right edge of the ribbon sample closest to the tile's own center.
        public static void CenterSpan(TileLayout tile, out Vector3 left, out Vector3 right)
        {
            int best = 0;
            float bestD = float.MaxValue;
            for (int k = 0; k < tile.edgeLeft.Count; k++)
            {
                Vector3 mid = (tile.edgeLeft[k] + tile.edgeRight[k]) * .5f;
                float dx = mid.x - tile.position.x, dz = mid.z - tile.position.z;
                float d = dx * dx + dz * dz;
                if (d < bestD) { bestD = d; best = k; }
            }
            left = tile.edgeLeft.Count > 0 ? tile.edgeLeft[best] : tile.position;
            right = tile.edgeRight.Count > 0 ? tile.edgeRight[best] : tile.position;
        }

        // A random point on the left-right segment, keeping `margin` meters from both ends, or
        // its middle when the segment is narrower than two margins.
        public static Vector3 Across(Vector3 left, Vector3 right, float margin, SeededRng rng)
        {
            float width = Vector3.Distance(left, right);
            float edge = width > 2f * margin ? margin / width : .5f;
            return Vector3.Lerp(left, right, rng.Range(edge, 1f - edge));
        }
    }
}
