using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SoulBlocks.Tests
{
    public class CrystalMiningTests
    {
        GameObject ground, actor, crystalObject;
        CrystalMiningAttack attack;
        BreakableCrystal crystal;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // A scene left loaded by an earlier test would put its terrain and paths under this
            // rig at the origin.
            var leftover = SceneManager.GetSceneByName(GameRoot.SceneName);
            if (leftover.IsValid() && leftover.isLoaded)
            {
                SceneManager.SetActiveScene(SceneManager.CreateScene("Crystal mining test " + Time.frameCount));
                yield return SceneManager.UnloadSceneAsync(leftover);
            }
            Keys.Blocked = false;
            Time.timeScale = 1;
            // A scene left loaded by an earlier test keeps its special blocks live: a random red
            // passive explosion would add damage to the swings counted here.
            if (SpecialBlockSystem.Instance != null) SpecialBlockSystem.Instance.PassivesEnabled = false;
            ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.layer = 8;
            ground.transform.position = new Vector3(0, -.5f, 0);
            ground.transform.localScale = new Vector3(20, 1, 20);
            actor = new GameObject("Mining test player");
            actor.SetActive(false);
            var controller = actor.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.center = Vector3.up * .9f;
            var player = actor.AddComponent<SoulPlayer>();
            player.readInput = false;
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(visual.GetComponent<Collider>());
            visual.transform.SetParent(actor.transform, false);
            player.visual = visual.transform;
            attack = actor.AddComponent<CrystalMiningAttack>();
            var volume = new GameObject("Hitbox");
            volume.transform.SetParent(actor.transform, false);
            attack.hitbox = volume.AddComponent<BoxCollider>();
            attack.hitbox.center = new Vector3(0, .55f, .9f);
            attack.hitbox.size = new Vector3(.85f, 1.1f, 1.3f);
            attack.hitbox.isTrigger = true;
            attack.hitbox.enabled = false;
            actor.SetActive(true);

            crystalObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crystalObject.transform.position = new Vector3(0, 0, 1.25f);
            crystal = crystalObject.AddComponent<BreakableCrystal>();
            Physics.SyncTransforms();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1;
            Keys.Blocked = false;
            Object.Destroy(actor);
            Object.Destroy(crystalObject);
            Object.Destroy(ground);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TenSeparateSwingsBreakYellowCrystalAndFlashBothRed()
        {
            Assert.AreEqual(10, crystal.Health);
            for (int i = 0; i < 10; i++)
            {
                Assert.IsTrue(attack.TrySwing(Vector3.forward));
                Assert.IsFalse(attack.TrySwing(Vector3.forward), "A repeated click must not overlap attacks.");
                yield return new WaitForSeconds(.25f);
                Assert.AreEqual(9 - i, crystal.Health, "Each swing applies exactly one damage.");
                AssertRed(crystal.GetComponent<Renderer>());
                AssertRed(actor.GetComponent<SoulPlayer>().visual.GetComponent<Renderer>());
                yield return new WaitForSeconds(.35f);
                Assert.IsFalse(attack.hitbox.enabled);
            }
            Assert.IsTrue(crystal == null, "Tenth hit must remove the crystal after its flash.");
        }

        [UnityTest]
        public IEnumerator OutOfReachAndBehindPlayerDoNotTakeDamage()
        {
            crystalObject.transform.position = Vector3.forward * 4;
            Assert.IsTrue(attack.TrySwing(Vector3.forward));
            yield return new WaitForSeconds(.6f);
            Assert.AreEqual(10, crystal.Health);
            crystalObject.transform.position = Vector3.back;
            Assert.IsTrue(attack.TrySwing(Vector3.forward));
            yield return new WaitForSeconds(.6f);
            Assert.AreEqual(10, crystal.Health);
        }

        [UnityTest]
        public IEnumerator MultipleCollidersStillTakeOnlyOneHit()
        {
            var extra = crystalObject.AddComponent<BoxCollider>();
            extra.center = Vector3.up * .4f;
            Assert.IsTrue(attack.TrySwing(Vector3.forward));
            yield return new WaitForSeconds(.6f);
            Assert.AreEqual(9, crystal.Health);
        }

        [UnityTest]
        public IEnumerator ModalCancelsSwingAndRestoresContact()
        {
            Assert.IsTrue(attack.TrySwing(Vector3.forward));
            Keys.Blocked = true;
            yield return null;
            Assert.IsFalse(attack.IsSwinging);
            Assert.IsFalse(attack.hitbox.enabled);
            Assert.AreEqual(10, crystal.Health);
            Assert.IsFalse(attack.TrySwing(Vector3.forward));
        }

        [UnityTest]
        public IEnumerator FlashRestoresOriginalPlayerColorAndCrystalYellow()
        {
            var renderer = actor.GetComponent<SoulPlayer>().visual.GetComponent<Renderer>();
            var original = new MaterialPropertyBlock();
            original.SetColor("_BaseColor", Color.green);
            renderer.SetPropertyBlock(original);
            attack.TrySwing(Vector3.forward);
            yield return new WaitForSeconds(.6f);
            var restored = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(restored);
            Assert.AreEqual(Color.green, restored.GetColor("_BaseColor"));
            crystal.GetComponent<Renderer>().GetPropertyBlock(restored);
            Color actual = restored.GetColor("_BaseColor");
            Assert.Less(Vector4.Distance(crystal.BaseColor, actual), .001f);
        }

        [TestCase(CrystalColor.Yellow, CrystalSize.Small, 10)]
        [TestCase(CrystalColor.Yellow, CrystalSize.Medium, 20)]
        [TestCase(CrystalColor.Blue, CrystalSize.Small, 150)]
        [TestCase(CrystalColor.Violet, CrystalSize.Large, 24)]
        public void ColorAndSizeDetermineHealth(CrystalColor color, CrystalSize size, int expected)
        {
            Assert.AreEqual(expected, BreakableCrystal.HealthFor(color, size));
        }

        static void AssertRed(Renderer renderer)
        {
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            Assert.AreEqual(Color.red, block.GetColor("_BaseColor"));
        }

        [UnityTest]
        public IEnumerator HeldMiningFollowsCameraAimDuringSwingAndAllowsStrafing()
        {
            var player = actor.GetComponent<SoulPlayer>();
            var cameraObject = new GameObject("Orbit camera");
            cameraObject.transform.SetParent(actor.transform, false);
            player.viewCamera = cameraObject.AddComponent<Camera>();
            attack.ProcessMiningInput(true, Vector3.forward);
            cameraObject.transform.rotation = Quaternion.Euler(15, 90, 0);
            attack.ProcessMiningInput(true, cameraObject.transform.forward);
            Vector3 before = actor.transform.position;
            player.StepMovement(Vector2.right, .2f);
            Assert.Greater(before.z - actor.transform.position.z, .9f, "Mining must allow lateral movement relative to the new aim.");
            Assert.Less(Mathf.Abs(actor.transform.position.x - before.x), .05f);
            Assert.Less(Mathf.Abs(Mathf.DeltaAngle(90, player.Yaw)), .01f);
            crystalObject.transform.position = actor.transform.position + Vector3.right * 1.25f;
            Physics.SyncTransforms();
            float deadline = Time.time + 8;
            while (crystal != null && Time.time < deadline)
            {
                attack.ProcessMiningInput(true, Vector3.right);
                Assert.Greater(Vector3.Dot(attack.MiningDirection, Vector3.right), .999f);
                yield return null;
            }
            Assert.IsTrue(crystal == null, "The moving hitbox must break the crystal ahead after ten repeated hits.");
            attack.ProcessMiningInput(false, Vector3.right);
        }

        [UnityTest]
        public IEnumerator ReleasingAndPressingAgainChoosesNewMiningDirection()
        {
            attack.ProcessMiningInput(true, Vector3.forward);
            attack.ProcessMiningInput(false, Vector3.forward);
            yield return new WaitForSeconds(.6f);
            Assert.IsFalse(attack.DirectionLocked);
            Assert.IsFalse(attack.IsSwinging);
            attack.ProcessMiningInput(true, Vector3.right);
            Assert.Greater(Vector3.Dot(attack.MiningDirection, Vector3.right), .999f);
            attack.ProcessMiningInput(false, Vector3.right);
        }
    }
}
