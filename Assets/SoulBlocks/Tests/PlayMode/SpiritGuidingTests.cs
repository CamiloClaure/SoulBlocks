using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SoulBlocks.Tests
{
    public class SpiritGuidingTests
    {
        GuidingTorch torch; SoulPlayer player; AuthoredPath path;
        [UnitySetUp] public IEnumerator Setup()
        {
            SceneManager.LoadScene("WorldBuilding");
            for(int i=0;i<15;i++) yield return null;
            torch=Object.FindAnyObjectByType<GuidingTorch>(); player=GameRoot.Instance.Player;
            player.readInput=false; path=GameObject.Find("Camino primario").GetComponent<AuthoredPath>();
            Assert.IsNotNull(torch); Assert.AreEqual(18,Object.FindObjectsByType<WanderingSpirit>(FindObjectsSortMode.None).Length);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale=1;Keys.Blocked=false;
            var empty=SceneManager.CreateScene("Spirit cleanup");SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("WorldBuilding");
        }
        [UnityTest] public IEnumerator CrystalsDoNotReleaseOldSoulPickups()
        {
            Assert.IsNull(GameObject.Find("Generated Souls"));
            int before=GameRoot.Instance.Run.collectedCrystals;
            var crystal=Object.FindObjectsByType<BreakableCrystal>(FindObjectsSortMode.None).First();
            int reward=3*(int)crystal.size;
            var color=crystal.color;
            player.Teleport(crystal.transform.position+Vector3.right*15);
            crystal.TakeDamage(1000);
            Assert.IsFalse(crystal.TakeDamage(1000),"A broken crystal must not spawn twice");
            yield return new WaitForSeconds(.8f);
            Assert.AreEqual(before,GameRoot.Instance.Run.collectedCrystals,"Breaking is not collecting");
            var fragments=Object.FindObjectsByType<CrystalFragment>(FindObjectsSortMode.None);
            Assert.AreEqual(reward,fragments.Length);
            foreach(var fragment in fragments)
            {
                Assert.AreEqual(color,fragment.Color);
                Assert.IsNotNull(fragment.model);
                Assert.IsFalse(fragment.TryCollect(player),"Cannot collect from far away");
            }
            Keys.Blocked=true;
            player.Teleport(fragments[0].transform.position);
            Assert.IsFalse(fragments[0].TryCollect(player));
            Keys.Blocked=false;
            foreach(var fragment in fragments)
            {
                player.Teleport(fragment.transform.position);
                Assert.IsTrue(fragment.TryCollect(player));
                Assert.IsFalse(fragment.TryCollect(player),"Cannot count twice");
            }
            Assert.AreEqual(before+reward,GameRoot.Instance.Run.collectedCrystals);
            yield return null;
            Assert.AreEqual(0,Object.FindObjectsByType<CrystalFragment>(FindObjectsSortMode.None).Length);
            Assert.AreEqual(0,Object.FindObjectsByType<SoulPickup>(FindObjectsSortMode.None).Length);
            foreach(var spirit in Object.FindObjectsByType<WanderingSpirit>(FindObjectsSortMode.None))
            {
                Assert.AreEqual(1,spirit.visual.childCount);
                Assert.GreaterOrEqual(spirit.wanderSpeed,4);
                Assert.Greater(spirit.followSpeed,player.moveSpeed);
                Assert.IsTrue(spirit.visual.GetComponentInChildren<Renderer>().sharedMaterial.IsKeywordEnabled("_EMISSION"));
            }
        }
        [UnityTest] public IEnumerator TorchCanBeCarriedPlacedRetrievedAndDocked()
        {
            Assert.IsFalse(torch.Lit);
            player.Teleport(torch.AltarPosition+Vector3.right*1.7f);
            Assert.IsTrue(torch.Interact(player)); yield return null; yield return null;
            Assert.AreEqual(TorchState.Carried,torch.State); Assert.IsTrue(torch.Lit);
            Assert.AreEqual(player.TorchHand != null ? player.TorchHand : player.transform,torch.transform.parent);
            Assert.AreEqual(1,torch.transform.lossyScale.x,.02f);
            var mining=player.GetComponent<CrystalMiningAttack>();
            Assert.IsTrue(mining.TrySwing(Vector3.forward));
            yield return new WaitForSeconds(.6f);
            Assert.AreEqual(TorchState.Carried,torch.State);
            Assert.AreEqual(player.TorchHand != null ? player.TorchHand : player.transform,torch.transform.parent);
            player.Teleport(path.transform.TransformPoint(path.Evaluate(.25f))+Vector3.up*.1f); yield return null;
            Assert.IsTrue(torch.Interact(player)); Assert.AreEqual(TorchState.Placed,torch.State);
            Vector3 placed=torch.transform.position;
            player.Teleport(player.transform.position+Vector3.back*5);yield return new WaitForSeconds(.3f);
            Assert.Less(Vector3.Distance(Vector3.ProjectOnPlane(placed,Vector3.up),Vector3.ProjectOnPlane(torch.transform.position,Vector3.up)),.01f);
            Assert.IsFalse(torch.Interact(player),"Cannot retrieve at a distance");
            Capture();
            player.Teleport(placed+Vector3.right);Assert.IsTrue(torch.Interact(player));yield return null;
            player.Teleport(torch.AltarPosition+Vector3.right*1.7f);Assert.IsTrue(torch.Interact(player));yield return null;
            Assert.AreEqual(TorchState.Altar,torch.State); Assert.IsTrue(torch.Lit);
            Assert.AreEqual(1,Object.FindObjectsByType<GuidingTorch>(FindObjectsSortMode.None).Length);
        }
        [UnityTest] public IEnumerator LightAttractsAndAltarReceivesExactlyOnce()
        {
            var spirit=Object.FindObjectsByType<WanderingSpirit>(FindObjectsSortMode.None).First();
            foreach(var other in Object.FindObjectsByType<WanderingSpirit>(FindObjectsSortMode.None))
                if(other != spirit) other.gameObject.SetActive(false);
            player.Teleport(torch.AltarPosition+Vector3.right*1.7f);torch.Interact(player);
            player.Teleport(path.transform.TransformPoint(path.Evaluate(.3f))+Vector3.up*.1f);yield return null;
            Assert.IsTrue(torch.Interact(player));
            torch.TickCall(12);
            spirit.transform.position=torch.transform.position+Vector3.right*5;
            float before=Vector3.Distance(spirit.transform.position,torch.transform.position);
            yield return new WaitForSeconds(.5f);
            Assert.IsTrue(spirit.Following);Assert.Less(Vector3.Distance(spirit.transform.position,torch.transform.position),before);
            torch.SetLit(false);yield return null;Assert.IsFalse(spirit.Following);
            torch.SetLit(true);yield return null;Assert.IsTrue(spirit.Following);
            Keys.Blocked=true;var stopped=spirit.transform.position;yield return new WaitForSeconds(.2f);Assert.AreEqual(stopped,spirit.transform.position);Keys.Blocked=false;
            player.Teleport(torch.transform.position+Vector3.right);torch.Interact(player);yield return null;
            player.Teleport(torch.AltarPosition+Vector3.right*1.7f);torch.Interact(player);
            spirit.transform.position=torch.AltarPosition+Vector3.forward;
            int souls=GameRoot.Instance.Run.reserveSouls;
            yield return new WaitForSeconds(1.8f);
            Assert.IsTrue(spirit==null);Assert.AreEqual(1,torch.GuidedSouls);Assert.AreEqual(souls+1,GameRoot.Instance.Run.reserveSouls);
            yield return new WaitForSeconds(.2f);Assert.AreEqual(1,torch.GuidedSouls);
        }
        [UnityTest] public IEnumerator PlacedTorchGrowsInPulsesAndResetsWhenPlacedAgain()
        {
            player.Teleport(torch.AltarPosition+Vector3.right*1.7f);torch.Interact(player);
            torch.TickCall(30);Assert.AreEqual(4,torch.CurrentRadius);
            player.Teleport(path.transform.TransformPoint(path.Evaluate(.65f))+Vector3.up*.1f);yield return null;
            Assert.IsTrue(torch.Interact(player));Assert.AreEqual(4,torch.CurrentRadius);
            torch.TickCall(2);Assert.AreEqual(4,torch.CurrentRadius);
            torch.TickCall(1);Assert.AreEqual(6,torch.CurrentRadius);
            torch.SetLit(false);torch.TickCall(50);Assert.AreEqual(6,torch.CurrentRadius);
            torch.SetLit(true);torch.TickCall(9);Assert.AreEqual(12,torch.CurrentRadius);Assert.IsTrue(torch.ReadyToRetrieve);
            torch.TickCall(100);Assert.AreEqual(12,torch.CurrentRadius);
            Assert.IsTrue(torch.Calls(torch.LightPosition+Vector3.right*10,false));
            player.Teleport(torch.transform.position+Vector3.right);Assert.IsTrue(torch.Interact(player));
            Assert.IsFalse(torch.ReadyToRetrieve);Assert.AreEqual(4,torch.CurrentRadius);
            Assert.IsTrue(torch.Calls(torch.LightPosition+Vector3.right*10,true),"Collected spirits retain follow range");
            Assert.IsFalse(torch.Calls(torch.LightPosition+Vector3.right*10,false));
            Assert.IsTrue(torch.Interact(player));Assert.AreEqual(4,torch.CurrentRadius);
        }
        [UnityTest] public IEnumerator WanderingSpheresStayOutsideTheRoad()
        {
            yield return new WaitForSeconds(1);
            foreach(var spirit in Object.FindObjectsByType<WanderingSpirit>(FindObjectsSortMode.None))
            {
                Assert.IsTrue(spirit.visual.GetComponentInChildren<MeshFilter>().sharedMesh.name.ToLower().Contains("sphere"));
                for(int i=0;i<=500;i++)
                {
                    var delta=path.transform.TransformPoint(path.Evaluate(i/500f))-spirit.transform.position;delta.y=0;
                    Assert.Greater(delta.magnitude,path.width*.5f,"An idle spirit entered the road");
                }
            }
        }
        void Capture()
        {
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return;
            var go=new GameObject("Torch preview");var camera=go.AddComponent<Camera>();camera.transform.position=torch.transform.position+new Vector3(7,5,-7);camera.transform.LookAt(torch.transform.position);camera.fieldOfView=55;
            var target=new RenderTexture(1200,900,24);var previous=RenderTexture.active;camera.targetTexture=target;camera.Render();RenderTexture.active=target;
            var image=new Texture2D(1200,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1200,900),0,0);image.Apply();System.IO.Directory.CreateDirectory("Validation");System.IO.File.WriteAllBytes("Validation/torch-guide.png",image.EncodeToPNG());
            RenderTexture.active=previous;camera.targetTexture=null;Object.Destroy(image);Object.Destroy(target);Object.Destroy(go);
        }
    }
}
