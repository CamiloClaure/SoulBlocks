using UnityEngine;

namespace SoulBlocks
{
    public sealed class SpecialBlockPedestal : MonoBehaviour
    {
        public SpecialBlock block;
        public TextMesh label;
        void LateUpdate()
        {
            if (label == null) return;
            var system = SpecialBlockSystem.Instance;
            label.text = BlockRules.Name(block) + "\nNv " + (system != null ? system.Level(block) : 1);
            if (Camera.main != null) label.transform.rotation = Camera.main.transform.rotation;
        }
    }
}
