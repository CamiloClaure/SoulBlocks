using UnityEngine;

namespace SoulBlocks
{
    // Keep the original serialized values intact.
    public enum CrystalColor { Yellow = 0, Blue = 1, Violet = 2, Green = 3, Celeste = 4, Red = 5 }
    public enum CrystalSize { Small = 1, Medium = 2, Large = 3 }

    [DisallowMultipleComponent, RequireComponent(typeof(BoxCollider))]
    public sealed class BreakableCrystal : MonoBehaviour
    {
        [Header("Resistencia: vida del color × tamaño")]
        public CrystalColor color = CrystalColor.Yellow;
        public CrystalSize size = CrystalSize.Small;
        public bool startsHidden;
        public int rewardSouls;
        [Tooltip("Prefab de los fragmentos recolectables. Si no se asigna, usa Resources/Crystals/CrystalFragment.")]
        public CrystalFragment fragmentPrefab;
        public bool Revealed { get; private set; }
        public bool Available => isActiveAndEnabled && !Broken && Revealed;
        public static readonly System.Collections.Generic.List<BreakableCrystal> All = new System.Collections.Generic.List<BreakableCrystal>();
        [Tooltip("Reemplaza el hijo Placeholder por tu modelo de Blender dentro de Model.")]
        public Transform model;
        public const float HitFlashSeconds = .16f;
        public int MaxHealth => HealthFor(color, size);
        public int Health { get; private set; }
        public bool Broken => Health <= 0;
        float flashUntil;
        Renderer[] renderers;
        BoxCollider body;
        bool inverted;

        public static int HealthFor(CrystalColor color, CrystalSize size)
        {
            int baseHealth = color == CrystalColor.Blue ? 5 : color == CrystalColor.Violet ? 8
                : color == CrystalColor.Green ? 6 : color == CrystalColor.Celeste ? 7 : color == CrystalColor.Red ? 9 : 3;
            return baseHealth * Mathf.Max(1, (int)size);
        }

        public Color BaseColor => color == CrystalColor.Blue ? BlockRules.ColorOf(SpecialBlock.Blue)
            : color == CrystalColor.Violet ? new Color(.7f, .25f, 1f)
            : color == CrystalColor.Green ? BlockRules.ColorOf(SpecialBlock.Green)
            : color == CrystalColor.Celeste ? BlockRules.ColorOf(SpecialBlock.Celeste)
            : color == CrystalColor.Red ? BlockRules.ColorOf(SpecialBlock.Red) : new Color(1f, .8f, .05f);

        void Awake()
        {
            ResetCrystal();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ClearRegistry() => All.Clear();
        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable() => All.Remove(this);

        public void ResetCrystal()
        {
            ConfigureAppearance();
            Health = MaxHealth;
            flashUntil = 0;
            inverted = false;
            Revealed = !startsHidden;
            body.enabled = Revealed;
            foreach (var renderer in renderers) renderer.enabled = Revealed;
        }

        public void Reveal()
        {
            if (Broken || Revealed) return;
            Revealed = true;
            body.enabled = true;
            foreach (var renderer in renderers) renderer.enabled = true;
        }

        public void SetInverted(bool value)
        {
            if (inverted == value) return;
            inverted = value;
            if (flashUntil <= 0) Tint(IdleColor);
        }

        Color IdleColor => inverted ? new Color(1 - BaseColor.r, 1 - BaseColor.g, 1 - BaseColor.b) : BaseColor;

        void OnValidate() => ConfigureAppearance();

        public void ConfigureAppearance()
        {
            body = GetComponent<BoxCollider>();
            Vector3 dimensions = new Vector3(.55f, .9f, .55f) * Mathf.Max(1, (int)size);
            if (body != null)
            {
                body.size = dimensions;
                body.center = Vector3.up * dimensions.y * .5f;
            }
            if (model != null)
            {
                model.localPosition = Vector3.up * dimensions.y * .5f;
                model.localScale = dimensions;
            }
            renderers = model != null ? model.GetComponentsInChildren<Renderer>(true) : GetComponentsInChildren<Renderer>(true);
            Tint(BaseColor);
        }

        public bool TakeHit() => TakeDamage(1);

        public bool TakeDamage(int damage)
        {
            if (!Available || damage <= 0) return false;
            Health = Mathf.Max(0, Health - damage);
            flashUntil = Time.time + HitFlashSeconds;
            Tint(Color.red);
            // Disable contact immediately, but leave the final red flash visible before removal.
            if (Broken)
            {
                body.enabled = false;
                CrystalFragment.Scatter(this);
            }
            return true;
        }

        void Update()
        {
            if (flashUntil <= 0f || Time.time < flashUntil) return;
            flashUntil = 0f;
            if (Broken) Destroy(gameObject);
            else Tint(IdleColor);
        }

        void Tint(Color tint)
        {
            if (renderers == null) return;
            var block = new MaterialPropertyBlock();
            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;
                renderer.GetPropertyBlock(block);
                block.SetColor("_BaseColor", tint);
                block.SetColor("_Color", tint);
                renderer.SetPropertyBlock(block);
            }
        }
    }
}
