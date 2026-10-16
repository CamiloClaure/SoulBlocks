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
        [Header("Barra de vida")]
        public bool showHealthBar = true;
        [Min(1)] public float healthBarDistance = 18f;
        public float HealthFraction => Mathf.Clamp01((float)Health / Mathf.Max(1, MaxHealth));
        public bool ShowsHealthAt(Vector3 viewer) => showHealthBar && Available &&
            (viewer - transform.position).sqrMagnitude <= healthBarDistance * healthBarDistance;
        public bool Broken => Health <= 0;
        float flashUntil;
        Renderer[] renderers;
        BoxCollider body;
        bool inverted;
        public bool Nourished { get; private set; }
        public int FragmentReward { get; private set; }
        float bonusPercent, bonusChance;
        GameObject rewardAura;
        Material auraMaterial;

        public void Nourish(float percent, float chance)
        {
            if (!Available) return;
            bonusPercent = Mathf.Max(bonusPercent, percent);
            bonusChance = Mathf.Clamp01(chance);
            if (Nourished) return;
            Nourished = true;
            rewardAura = new GameObject("Aura de recompensa azul");
            rewardAura.transform.SetParent(transform, false);
            var line = rewardAura.AddComponent<LineRenderer>();
            auraMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            auraMaterial.SetColor("_BaseColor", new Color(.2f, .7f, 1));
            line.sharedMaterial = auraMaterial;
            line.useWorldSpace = false;
            line.startWidth = line.endWidth = .07f;
            line.positionCount = 49;
            float radius = .5f * Mathf.Max(1, (int)size);
            for (int i = 0; i <= 48; i++)
            {
                float angle = i * Mathf.PI * 2 / 48;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, .45f * (int)size, Mathf.Sin(angle) * radius));
            }
            var glow = rewardAura.AddComponent<Light>();
            glow.color = new Color(.2f, .6f, 1); glow.range = radius * 5; glow.intensity = 2;
        }
        void OnDestroy() { if (auraMaterial != null) Destroy(auraMaterial); }

        public static int HealthFor(CrystalColor color, CrystalSize size)
        {
            return MiningBalance.Current.Health(color) * Mathf.Max(1, (int)size);
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
        void OnEnable() { if (!All.Contains(this)) All.Add(this); Hud.Register(320, DrawHealthBar); }
        void OnDisable() { All.Remove(this); Hud.Unregister(DrawHealthBar); }

        void DrawHealthBar()
        {
            var root = GameRoot.Instance;
            if (Keys.Blocked || root == null || root.Player == null || root.ViewCamera == null || body == null ||
                !ShowsHealthAt(root.Player.transform.position)) return;
            var bounds = body.bounds;
            var anchor = new Vector3(bounds.center.x, bounds.max.y + .3f, bounds.center.z);
            var point = root.ViewCamera.WorldToScreenPoint(anchor);
            if (point.z <= 0 || point.x < 0 || point.x > Screen.width || point.y < 0 || point.y > Screen.height) return;
            float x = point.x - 46, y = Screen.height - point.y;
            var old = GUI.color;
            GUI.color = Hud.PanelColor;
            GUI.DrawTexture(new Rect(x, y, 92, 10), Texture2D.whiteTexture);
            GUI.color = BaseColor;
            GUI.DrawTexture(new Rect(x + 2, y + 2, 88 * HealthFraction, 6), Texture2D.whiteTexture);
            GUI.color = Color.white;
            Hud.TextBox(new Rect(point.x, y - 14, 0, 0), Health + " / " + MaxHealth, TextAnchor.MiddleCenter);
            GUI.color = old;
        }

        public void ResetCrystal()
        {
            if (rewardAura != null) Destroy(rewardAura);
            if (auraMaterial != null) Destroy(auraMaterial);
            Nourished = false; bonusPercent = bonusChance = 0;
            FragmentReward = 3 * Mathf.Max(1, (int)size);
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
                if (Nourished && (SpecialBlockSystem.Instance != null ? SpecialBlockSystem.Instance.RewardRoll() : Random.value) < bonusChance)
                    FragmentReward += Mathf.CeilToInt(FragmentReward * bonusPercent);
                CrystalFragment.Scatter(this);
            }
            return true;
        }

        void Update()
        {
            if (rewardAura != null) rewardAura.transform.localPosition = Vector3.up * Mathf.Sin(Time.time * 3) * .12f;
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
