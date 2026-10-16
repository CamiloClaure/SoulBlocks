using UnityEngine;
namespace SoulBlocks
{
    public sealed class BlockLabInteraction : MonoBehaviour
    {
        void Update()
        {
            if (TreeUpgradeAltar.Instance != null) return;
            var root=GameRoot.Instance;var system=SpecialBlockSystem.Instance;
            if(root==null || root.Player==null || system==null || !system.Ready || system.ShopOpen)return;
            if(Vector3.Distance(root.Player.transform.position,transform.position)<15 && Keys.InteractPressed)system.OpenShop();
        }
    }
}
