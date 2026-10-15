using NUnit.Framework;
using UnityEngine;

namespace SoulBlocks.Tests
{
    public class AuthoredPathTests
    {
        [Test]
        public void CurvePassesThroughPointsAndColliderFollowsEdits()
        {
            var go = new GameObject("Path test");
            try
            {
                var path = go.AddComponent<AuthoredPath>();
                path.Rebuild();
                for (int i = 0; i < path.points.Count; i++) Assert.Less(Vector3.Distance(path.points[i], path.Evaluate((float)i / (path.points.Count - 1))), .001f);
                Physics.SyncTransforms();
                Assert.IsTrue(Physics.Raycast(path.Evaluate(.5f) + Vector3.up * 3, Vector3.down, out var hit, 5, 1 << 8));
                Assert.AreEqual(go.GetComponent<MeshCollider>(), hit.collider);
                path.points[1] += Vector3.up * 2;
                path.Rebuild(); Physics.SyncTransforms();
                Assert.IsTrue(Physics.Raycast(path.points[1] + Vector3.up * 3, Vector3.down, out hit, 5, 1 << 8));
                Assert.AreEqual(path.points[1].y, hit.point.y, .05f);
                Assert.Greater(hit.normal.y, .5f);
                path.points.Clear(); path.Rebuild();
                Assert.IsNull(go.GetComponent<MeshCollider>().sharedMesh);
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
