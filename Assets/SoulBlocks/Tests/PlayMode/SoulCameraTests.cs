using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SoulBlocks.Tests
{
    public class SoulCameraTests
    {
        GameObject actor, cameraObject, ground, wall;
        SoulPlayer player;
        SoulCamera rig;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Keys.Blocked = false;
            ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.layer = 8;
            ground.transform.position = Vector3.down * .5f;
            ground.transform.localScale = new Vector3(100, 1, 100);
            actor = new GameObject("Camera test player");
            var controller = actor.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.center = Vector3.up * .9f;
            player = actor.AddComponent<SoulPlayer>();
            player.readInput = false;
            player.visual = new GameObject("Visual").transform;
            player.visual.SetParent(actor.transform, false);
            player.visual.rotation = Quaternion.Euler(0, 90, 0);
            cameraObject = new GameObject("Camera test");
            player.viewCamera = cameraObject.AddComponent<Camera>();
            rig = cameraObject.AddComponent<SoulCamera>();
            rig.player = actor.transform;
            rig.readInput = false;
            rig.autoCenter = true; // Exercise the optional centering mode as well.
            yield return null;
            rig.enabled = false; // Tests advance camera time explicitly.
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Keys.Blocked = false;
            Object.Destroy(actor);
            Object.Destroy(cameraObject);
            Object.Destroy(ground);
            if (wall != null) Object.Destroy(wall);
            yield return null;
        }

        [Test]
        public void StartsBehindAuthoredFacingAndFramesPlayer()
        {
            Assert.Less(Mathf.Abs(Mathf.DeltaAngle(90, rig.orbitYaw)), .01f);
            Assert.Less(cameraObject.transform.position.x, actor.transform.position.x);
            Vector3 screen = player.viewCamera.WorldToViewportPoint(actor.transform.position + Vector3.up);
            Assert.That(screen.x, Is.InRange(.4f, .6f));
            Assert.That(screen.y, Is.InRange(.2f, .7f));
            Assert.Greater(screen.z, 0);
        }

        [Test]
        public void OrbitRespectsManualViewThenRecentersBehindFacing()
        {
            rig.RotateOrbit(new Vector2(90, 0));
            for (int i = 0; i < 60; i++) rig.StepCamera(1f / 60);
            Assert.Less(Mathf.Abs(Mathf.DeltaAngle(180, rig.orbitYaw)), .01f);
            rig.RecenterBehindPlayer();
            for (int i = 0; i < 120; i++) rig.StepCamera(1f / 60);
            Assert.Less(Mathf.Abs(Mathf.DeltaAngle(player.Yaw, rig.orbitYaw)), .1f);
        }

        [Test]
        public void SidewaysMovementDoesNotCauseCameraFeedback()
        {
            float yaw = rig.orbitYaw;
            for (int i = 0; i < 120; i++)
            {
                player.StepMovement(Vector2.right, 1f / 60);
                rig.StepCamera(1f / 60);
            }
            Assert.Less(Mathf.Abs(Mathf.DeltaAngle(yaw, rig.orbitYaw)), .01f);
            player.StepMovement(Vector2.zero, 1f / 60);
            for (int i = 0; i < 120; i++) rig.StepCamera(1f / 60);
            Assert.Less(Mathf.Abs(Mathf.DeltaAngle(player.Yaw, rig.orbitYaw)), .1f);
        }

        [Test]
        public void WallPullsCameraInAndRemovingWallRestoresDistance()
        {
            rig.orbitYaw = 0;
            rig.SnapToPlayer();
            Vector3 unobstructed = cameraObject.transform.position;
            wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = new Vector3(0, 2, -2);
            wall.transform.localScale = new Vector3(6, 5, .3f);
            Physics.SyncTransforms();
            rig.SnapToPlayer();
            Assert.Greater(cameraObject.transform.position.z, -1.85f, "Camera must stay in front of wall.");
            wall.SetActive(false);
            Physics.SyncTransforms();
            rig.SnapToPlayer();
            Assert.Less(Vector3.Distance(unobstructed, cameraObject.transform.position), .001f);
        }

        [Test]
        public void OrbitAndZoomStayWithinLimits()
        {
            rig.RotateOrbit(new Vector2(10000, 10000));
            rig.orbitDistance = -1;
            rig.SnapToPlayer();
            Assert.AreEqual(rig.minPitch, rig.orbitPitch);
            Assert.AreEqual(rig.minDistance, rig.orbitDistance);
            rig.RotateOrbit(new Vector2(0, -10000));
            rig.orbitDistance = 100;
            rig.SnapToPlayer();
            Assert.AreEqual(rig.maxPitch, rig.orbitPitch);
            Assert.AreEqual(rig.maxDistance, rig.orbitDistance);
        }

        [Test]
        public void ModalDoesNotRecenterCamera()
        {
            rig.RotateOrbit(new Vector2(90, 0));
            Keys.Blocked = true;
            for (int i = 0; i < 300; i++) rig.StepCamera(1f / 60);
            Assert.Less(Mathf.Abs(Mathf.DeltaAngle(180, rig.orbitYaw)), .01f);
        }

        [Test]
        public void FreeLookKeepsChosenDirectionWhileIdle()
        {
            rig.autoCenter = false;
            rig.RotateOrbit(new Vector2(70, 0));
            for (int i = 0; i < 600; i++) rig.StepCamera(1f / 60);
            Assert.Less(Mathf.Abs(Mathf.DeltaAngle(160, rig.orbitYaw)), .01f);
        }

        [Test]
        public void MenusAndFocusLossReleaseCursor()
        {
            rig.readInput = true;
            Keys.Blocked = true;
            rig.UpdateCursorState(true);
            Assert.AreEqual(CursorLockMode.None, Cursor.lockState);
            Assert.IsTrue(Cursor.visible);
            Keys.Blocked = false;
            rig.UpdateCursorState(false);
            Assert.AreEqual(CursorLockMode.None, Cursor.lockState);
            Assert.IsTrue(Cursor.visible);
        }
    }
}
