using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SoulBlocks
{
    // Decor: plain walkable ground. Branch: an expedition branch (nested under another Branch
    // path it extends that branch). Connection: a bridge between the two branches nearest its
    // ends. SceneLayout reads these to build the run's MapLayout.
    public enum PathRole { Decor, Branch, Connection }

    // A walkable ribbon drawn by hand: a Catmull-Rom curve through `points` (local space) with
    // a slab mesh and MeshCollider on the Ground layer, rebuilt whenever it changes, in edit
    // mode too. AuthoredPathEditor adds the point handles.
    [ExecuteAlways, RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public sealed class AuthoredPath : MonoBehaviour
    {
        public List<Vector3> points = new List<Vector3> { Vector3.zero, new Vector3(0, 0, 8), new Vector3(4, 1, 16), new Vector3(0, 2, 24) };
        [Min(.5f)] public float width = 4;
        [Min(.05f)] public float thickness = .5f;
        [Range(4, 64)] public int samplesPerSegment = 16;
        [Tooltip("Decor: solo suelo. Branch: rama de expedición (anidada bajo otra Branch la extiende). Connection: puente entre las dos ramas más cercanas a sus extremos.")]
        public PathRole role;
        public BranchSpecialty specialty;
        Mesh mesh;
        bool dirty = true;
        void OnEnable() => dirty = true;
        void OnValidate() => dirty = true;
        void Update() { if (dirty) Rebuild(); }
        void OnDestroy() { if (mesh != null) { if (Application.isPlaying) Destroy(mesh); else DestroyImmediate(mesh); } }

        public Vector3 Evaluate(float t)
        {
            if (points == null || points.Count == 0) return Vector3.zero;
            if (points.Count == 1) return points[0];
            float scaled = Mathf.Clamp01(t) * (points.Count - 1);
            int i = Mathf.Min(Mathf.FloorToInt(scaled), points.Count - 2);
            float u = scaled - i;
            Vector3 b = points[i], c = points[i + 1];
            Vector3 a = i > 0 ? points[i - 1] : 2 * b - c;
            Vector3 d = i + 2 < points.Count ? points[i + 2] : 2 * c - b;
            return .5f * ((2 * b) + (-a + c) * u + (2 * a - 5 * b + 4 * c - d) * u * u + (-a + 3 * b - 3 * c + d) * u * u * u);
        }

        public void Rebuild()
        {
            dirty = false;
            width = Mathf.Max(.5f, width); thickness = Mathf.Max(.05f, thickness);
            samplesPerSegment = Mathf.Clamp(samplesPerSegment, 4, 64);
            var filter = GetComponent<MeshFilter>();
            var collider = GetComponent<MeshCollider>();
            collider.sharedMesh = null;
            if (mesh == null) mesh = new Mesh { name = "Suelo del camino", hideFlags = HideFlags.DontSave, indexFormat = IndexFormat.UInt32 };
            mesh.Clear();
            filter.sharedMesh = mesh;
            if (points == null || points.Count < 2) return;
            int samples = (points.Count - 1) * samplesPerSegment;
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            void Tri(Vector3 a, Vector3 b, Vector3 c)
            {
                vertices.Add(a); vertices.Add(b); vertices.Add(c);
                uv.Add(new Vector2(a.x, a.z)); uv.Add(new Vector2(b.x, b.z)); uv.Add(new Vector2(c.x, c.z));
            }
            var left = new Vector3[samples + 1]; var right = new Vector3[samples + 1];
            Vector3 side = Vector3.right;
            for (int i = 0; i <= samples; i++)
            {
                float t = (float)i / samples;
                Vector3 tangent = Evaluate(Mathf.Min(1, t + .001f)) - Evaluate(Mathf.Max(0, t - .001f));
                Vector3 candidate = Vector3.Cross(Vector3.up, tangent);
                if (candidate.sqrMagnitude > .000001f) side = candidate.normalized;
                Vector3 p = Evaluate(t);
                left[i] = p - side * width * .5f; right[i] = p + side * width * .5f;
            }
            Vector3 down = Vector3.down * thickness;
            for (int i = 0; i < samples; i++)
            {
                Vector3 a = left[i], b = left[i + 1], c = right[i], d = right[i + 1];
                Tri(a, b, c); Tri(c, b, d);
                Tri(a + down, c + down, b + down); Tri(c + down, d + down, b + down);
                Tri(a, a + down, b); Tri(b, a + down, b + down);
                Tri(c, d, c + down); Tri(d, d + down, c + down);
            }
            Tri(left[0], right[0], left[0] + down); Tri(right[0], right[0] + down, left[0] + down);
            Tri(right[samples], left[samples], right[samples] + down); Tri(left[samples], left[samples] + down, right[samples] + down);
            var indices = new int[vertices.Count]; for (int i = 0; i < indices.Length; i++) indices[i] = i;
            mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(indices, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            collider.sharedMesh = mesh;
            gameObject.layer = 8;
        }
    }
}
