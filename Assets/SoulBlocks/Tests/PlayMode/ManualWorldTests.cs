using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SoulBlocks.Tests
{
    public class ManualWorldTests
    {
        [UnityTest]
        public IEnumerator ManualSceneLoadsWithoutGeneratedMapAndSupportsPlayer()
        {
            SceneManager.LoadScene("WorldBuilding");
            for (int i = 0; i < 15; i++) yield return null;
            var root = GameRoot.Instance;
            Assert.IsTrue(root.ManualWorld);
            root.Player.readInput = false;
            Assert.IsNull(GameObject.Find("Generated Map"));
            Assert.AreEqual(0, root.Layout.tiles.Count);
            Assert.IsFalse(Keys.Blocked);
            var path = GameObject.Find("Camino primario").GetComponent<AuthoredPath>();
            for (int i = 0; i <= 20; i++) Assert.IsTrue(SoulPlayer.IsSupportedAt(path.transform.TransformPoint(path.Evaluate(i / 20f))), "No floor at " + i);
            root.Player.Teleport(path.transform.TransformPoint(path.Evaluate(.7f)) + Vector3.up * .1f);
            yield return new WaitForSeconds(.5f);
            Assert.IsFalse(root.Run.inExpedition);
            for (int i = 71; i <= 90; i++)
            {
                Vector3 target = path.transform.TransformPoint(path.Evaluate(i / 100f));
                int steps = 0;
                while (Vector3.ProjectOnPlane(target - root.Player.transform.position, Vector3.up).magnitude > .25f && steps++ < 60)
                {
                    Vector3 direction = Vector3.ProjectOnPlane(target - root.Player.transform.position, Vector3.up).normalized;
                    float yaw = root.Player.viewCamera != null ? root.Player.viewCamera.transform.eulerAngles.y : root.Player.Yaw;
                    direction = Quaternion.Euler(0, -yaw, 0) * direction;
                    root.Player.StepMovement(new Vector2(direction.x, direction.z), 1f / 60);
                    yield return null;
                }
                Assert.Less(steps, 60, "Player stuck on slope at " + i);
            }
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                var cameraObject = new GameObject("Preview");
                var camera = cameraObject.AddComponent<Camera>();
                var bounds = path.GetComponent<MeshRenderer>().bounds;
                camera.transform.position = bounds.center + new Vector3(35, 45, -35);
                camera.transform.LookAt(bounds.center);
                camera.orthographic = true; camera.orthographicSize = bounds.size.z * .65f;
                var target = new RenderTexture(1200, 900, 24); var previous = RenderTexture.active;
                camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                var image = new Texture2D(1200, 900, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 1200, 900), 0, 0); image.Apply();
                System.IO.Directory.CreateDirectory("Validation");
                System.IO.File.WriteAllBytes("Validation/manual-world.png", image.EncodeToPNG());
                RenderTexture.active = previous; camera.targetTexture = null;
                Object.Destroy(image); Object.Destroy(target); Object.Destroy(cameraObject);
            }
            var empty = SceneManager.CreateScene("Manual cleanup"); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("WorldBuilding");
        }
    }
}
