using UnityEngine;

namespace SoulBlocks
{
    public sealed class CentralSpiritTree : MonoBehaviour
    {
        [Tooltip("Sustituye el bloque blanco dentro de Model por tu FBX.")]
        public Transform model;
        [Tooltip("Opcional: la futura animación puede usar un parámetro entero Stage.")]
        public Animator animator;
        public int Stage { get; private set; }
        void OnEnable()
        {
            GameEvents.MapBuilt += OnMapBuilt;
            GameEvents.TreeStageChanged += SetStage;
        }
        void OnDisable()
        {
            GameEvents.MapBuilt -= OnMapBuilt;
            GameEvents.TreeStageChanged -= SetStage;
        }
        void OnMapBuilt(MapLayout layout) => SetStage(GameRoot.Instance != null ? GameRoot.Instance.Run.treeStage : 0);
        public void SetStage(int stage)
        {
            Stage = stage;
            if (animator == null || animator.runtimeAnimatorController == null) return;
            foreach (var parameter in animator.parameters)
                if (parameter.name == "Stage" && parameter.type == AnimatorControllerParameterType.Int)
                    animator.SetInteger("Stage", stage);
        }
    }
}
