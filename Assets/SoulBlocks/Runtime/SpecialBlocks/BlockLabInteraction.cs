using UnityEngine;

namespace SoulBlocks
{
    // E near the block lab opens the special block shop, but only in a scene without the tree
    // altar (TreeUpgradeAltar owns E when there is one).
    public sealed class BlockLabInteraction : MonoBehaviour
    {
        void Update()
        {
            if (TreeUpgradeAltar.Instance != null) return;
            var root = GameRoot.Instance;
            var system = SpecialBlockSystem.Instance;
            if (root == null || root.Player == null || system == null || !system.Ready || system.ShopOpen) return;
            if (Vector3.Distance(root.Player.transform.position, transform.position) < 15 && Keys.InteractPressed) system.OpenShop();
        }
    }
}
