using UnityEngine;

namespace SoulBlocks
{
    // Purely visual: the soul, torch and reward logic stay on WanderingSpirit.
    public sealed class SpiritGlow : MonoBehaviour
    {
        public Transform halo;
        Vector3 restScale;
        float phase;
        void Start()
        {
            if (halo != null) restScale = halo.localScale;
            var spirit = GetComponentInParent<WanderingSpirit>();
            phase = spirit != null ? spirit.phase : transform.position.sqrMagnitude;
        }
        void Update()
        {
            if (halo != null && !Keys.Blocked)
                halo.localScale = restScale * (1 + Mathf.Sin(Time.time * 2.5f + phase) * .12f);
        }
    }
}
