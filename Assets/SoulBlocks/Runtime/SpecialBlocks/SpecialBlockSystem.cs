using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SoulBlocks
{
    public sealed partial class SpecialBlockSystem : MonoBehaviour
    {
        sealed class Area { public SpecialBlock kind; public Vector3 position; public float radius, until; public GameObject visual; }
        sealed class Bomb { public Vector3 position; public float until; public int level; public GameObject visual; }
        public static SpecialBlockSystem Instance { get; private set; }
        public SpecialBlockLab Lab { get; private set; }
        public bool Ready => Lab != null && GameRoot.Instance != null && GameRoot.Instance.Run != null;
        public bool PassivesEnabled = true;
        public int DeliveredSouls { get; set; }
        public int GreenHits { get; private set; }
        public int CelesteHits { get; private set; }
        public int GreenRolls { get; private set; }
        public int CelesteRolls { get; private set; }
        public int Explosions { get; private set; }
        public int ActiveBombs => bombs.Count;
        public int HelperCount { get { helpers.RemoveAll(h => h == null); return helpers.Count; } }
        public SpecialBlock Selected => Ready ? (SpecialBlock)Mathf.Clamp(Run.selectedSpecialBlock, 0, 4) : SpecialBlock.Yellow;
        public static float PlayerTempo => Instance != null && Instance.Ready && GameRoot.Instance.Player != null
            ? Instance.SpeedAt(GameRoot.Instance.Player.transform.position) : 1f;
        [NonSerialized] public Func<float> RandomValue;
        readonly List<Area> areas = new List<Area>();
        readonly List<Bomb> bombs = new List<Bomb>();
        readonly List<BlockHelper> helpers = new List<BlockHelper>();
        readonly float[] cooldowns = new float[5];
        RunState Run => GameRoot.Instance.Run;
        Vector3 lastPlayer;
        float steps, buffUntil, lastTempo = 1;
        SeededRng rng;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() => GameRoot.Register<SpecialBlockSystem>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Instance = null;
        void OnEnable()
        {
            Instance = this;
            GameEvents.MapBuilt += OnMapBuilt;
            Hud.Register(780, Draw);
        }
        void OnDisable()
        {
            GameEvents.MapBuilt -= OnMapBuilt;
            Hud.Unregister(Draw);
            if (shopOpen) CloseShop();
            if (GameRoot.Instance != null && GameRoot.Instance.Stats != null)
            {
                GameRoot.Instance.Stats.ClearSource("special.green");
                GameEvents.RaiseStatsChanged();
            }
            if (Instance == this) Instance = null;
        }
        void OnMapBuilt(MapLayout layout)
        {
            Lab = FindAnyObjectByType<SpecialBlockLab>();
            if (!Ready) return;
            if (Run.specialBlockLevels == null || Run.specialBlockLevels.Length != 5) Run.specialBlockLevels = new[] { 1, 1, 1, 1, 1 };
            for (int i = 0; i < 5; i++) Run.specialBlockLevels[i] = Mathf.Clamp(Run.specialBlockLevels[i], 1, BlockRules.MaxLevel);
            Run.specialBlockClothingLevel = Mathf.Clamp(Run.specialBlockClothingLevel, 0, BlockRules.MaxLevel);
            Run.selectedSpecialBlock = Mathf.Clamp(Run.selectedSpecialBlock, 0, 4);
            rng = SeededRng.Stream(Run.seed, "special.blocks");
            if (GameRoot.Instance.Player != null) lastPlayer = GameRoot.Instance.Player.transform.position;
        }
        public int Level(SpecialBlock block) => Ready ? Run.specialBlockLevels[(int)block] : 1;
        bool Roll() => (RandomValue != null ? RandomValue() : rng.NextFloat()) < BlockRules.Chance(Run.specialBlockClothingLevel);
        public void Select(int index) { if (Ready) Run.selectedSpecialBlock = ((index % 5) + 5) % 5; }

        void Update()
        {
            if (!Ready || GameRoot.Instance.Player == null) return;
            var root = GameRoot.Instance;
            if (shopOpen)
            {
                if (Time.frameCount == shopOpenedFrame) return;
                if (Keyboard.current != null && (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame)) CloseShop();
                return;
            }
            if (Keys.Blocked) { lastPlayer = root.Player.transform.position; return; }
            if (root.Player.readInput && Application.isFocused && !Keys.FreeCursorHeld && Mouse.current != null)
            {
                float wheel = Mouse.current.scroll.ReadValue().y;
                if (Mathf.Abs(wheel) > .01f) Select((int)Selected + (wheel > 0 ? 1 : -1));
                if (Mouse.current.rightButton.wasPressedThisFrame) TryUseSelected();
            }
            float tempo = SpeedAt(root.Player.transform.position);
            for (int i = 0; i < cooldowns.Length; i++) cooldowns[i] = Mathf.Max(0, cooldowns[i] - Time.deltaTime * tempo);
            float distance = Vector3.Distance(root.Player.transform.position, lastPlayer);
            lastPlayer = root.Player.transform.position;
            if (distance < 2f) steps += distance; // Teleports do not generate passive rolls.
            while (steps >= BlockRules.StepDistance) { steps -= BlockRules.StepDistance; NotifyStep(lastPlayer); }
            UpdateEffects();
            tempo = SpeedAt(root.Player.transform.position);
            if (Mathf.Abs(lastTempo - tempo) > .001f)
            {
                lastTempo = tempo;
                root.Stats.SetModifier("special.green", StatId.MoveSpeed, 0, tempo);
                GameEvents.RaiseStatsChanged();
            }
        }

        public bool TryUseSelected()
        {
            if (!Ready || Keys.Blocked || Time.timeScale <= 0 || cooldowns[(int)Selected] > 0) return false;
            var player = GameRoot.Instance.Player;
            if (player == null) return false;
            Vector3 forward = GameRoot.Instance.ViewCamera != null ? GameRoot.Instance.ViewCamera.transform.forward : player.transform.forward;
            forward.y = 0;
            forward.Normalize();
            Vector3 point = player.transform.position + forward * 1.5f;
            if (!SoulPlayer.IsSupportedAt(point)) point = player.transform.position;
            if (Physics.Raycast(point + Vector3.up * 2, Vector3.down, out var ground, 5, (1 << 8) | (1 << 9))) point.y = ground.point.y + .025f;
            int level = Level(Selected);
            switch (Selected)
            {
                case SpecialBlock.Yellow:
                    AddArea(Selected, point, BlockRules.Radius(Selected, level), BlockRules.Duration(Selected, level));
                    Illuminate(point, BlockRules.Radius(Selected, level), BlockRules.Duration(Selected, level));
                    break;
                case SpecialBlock.Blue:
                    AddArea(Selected, point, BlockRules.Radius(Selected, level), BlockRules.Duration(Selected, level));
                    Reveal(point, BlockRules.Radius(Selected, level));
                    break;
                case SpecialBlock.Green:
                    AddArea(Selected, point, BlockRules.Radius(Selected, level), BlockRules.Duration(Selected, level));
                    break;
                case SpecialBlock.Celeste:
                    if (HelperCount >= BlockRules.MaxHelpers) { Hud.Toast("Límite de 4 ayudantes activos"); return false; }
                    for (int i = 0; i < BlockRules.HelperCount(level); i++) SpawnHelper(point + Vector3.right * i * .4f);
                    break;
                case SpecialBlock.Red: PlaceBomb(point, level); break;
            }
            cooldowns[(int)Selected] = Selected == SpecialBlock.Celeste ? 10 : 5;
            Hud.Toast("Activo: " + BlockRules.Name(Selected));
            return true;
        }

        public void NotifyMiningHit(Vector3 position, bool helper)
        {
            if (!Ready || !PassivesEnabled) return;
            // Explosion damage never calls this method, so procs cannot recursively proc.
            if (Roll()) Explode(position, Level(SpecialBlock.Red), helper);
            GreenHits++;
            if (GreenHits >= BlockRules.HitInterval(Level(SpecialBlock.Green)))
            {
                GreenHits = 0; GreenRolls++;
                if (Roll()) { buffUntil = Time.time + BlockRules.BuffSeconds(Level(SpecialBlock.Green)); Hud.Toast("Pasiva verde: velocidad aumentada"); }
            }
            // Helpers benefit from green and red, but cannot recursively summon more helpers.
            if (helper) return;
            CelesteHits++;
            if (CelesteHits >= BlockRules.HitInterval(Level(SpecialBlock.Celeste)))
            {
                CelesteHits = 0; CelesteRolls++;
                if (Roll() && SpawnHelper(position)) Hud.Toast("Pasiva celeste: ayudante temporal");
            }
        }

        public void NotifyStep(Vector3 position)
        {
            if (Ready && PassivesEnabled && Roll())
            {
                Illuminate(position, BlockRules.Radius(SpecialBlock.Yellow, Level(SpecialBlock.Yellow)), 5 + Level(SpecialBlock.Yellow));
            }
        }
        void Illuminate(Vector3 position, float radius, float seconds)
        {
            foreach (var surface in Lab.surfaces)
            {
                if (surface == null || !surface.darkPath || Vector3.Distance(surface.transform.position, position) > radius) continue;
                bool newlyLit = !surface.Lit;
                surface.Illuminate(seconds);
                if (newlyLit && PassivesEnabled && Roll())
                {
                    Reveal(surface.transform.position, BlockRules.Radius(SpecialBlock.Blue, Level(SpecialBlock.Blue)));
                    Hud.Toast("Pasiva azul: revelación al iluminar");
                }
            }
        }
        public void Reveal(Vector3 position, float radius)
        {
            foreach (var crystal in BreakableCrystal.All)
                if (crystal != null && Vector3.Distance(position, crystal.transform.position) <= radius) crystal.Reveal();
        }
        public float SpeedAt(Vector3 position)
        {
            if (!Ready) return 1;
            if (Time.time < buffUntil) return BlockRules.Speed(Level(SpecialBlock.Green));
            foreach (var area in areas)
                if (area.kind == SpecialBlock.Green && Time.time < area.until && Vector3.Distance(position, area.position) <= area.radius)
                    return BlockRules.Speed(Level(SpecialBlock.Green));
            return 1;
        }
        bool InBlue(Vector3 position)
        {
            foreach (var area in areas)
                if (area.kind == SpecialBlock.Blue && Time.time < area.until && Vector3.Distance(position, area.position) <= area.radius) return true;
            return false;
        }
        void UpdateEffects()
        {
            for (int i = areas.Count - 1; i >= 0; i--)
                if (Time.time >= areas[i].until) { Destroy(areas[i].visual); areas.RemoveAt(i); }
            foreach (var surface in Lab.surfaces) if (surface != null) surface.Refresh(InBlue(surface.transform.position));
            foreach (var crystal in BreakableCrystal.All) if (crystal != null) crystal.SetInverted(InBlue(crystal.transform.position));
            for (int i = bombs.Count - 1; i >= 0; i--)
            {
                var bomb = bombs[i];
                float remaining = bomb.until - Time.time;
                if (remaining <= 0) { Explode(bomb.position, bomb.level, false); Destroy(bomb.visual); bombs.RemoveAt(i); continue; }
                Placeholder.SetColor(bomb.visual, Mathf.FloorToInt(Time.time * 10) % 2 == 0 ? Color.red : Color.white, 1);
            }
        }
        void AddArea(SpecialBlock kind, Vector3 point, float radius, float seconds)
        {
            var go = new GameObject(kind + " Area");
            go.transform.SetParent(transform);
            go.transform.position = point;
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = EffectMaterial;
            line.startColor = line.endColor = BlockRules.ColorOf(kind);
            var tint = new MaterialPropertyBlock();
            tint.SetColor("_BaseColor", BlockRules.ColorOf(kind));
            line.SetPropertyBlock(tint);
            line.startWidth = line.endWidth = .08f;
            line.positionCount = 65;
            for (int i = 0; i <= 64; i++)
            {
                float angle = i * Mathf.PI * 2 / 64;
                line.SetPosition(i, point + new Vector3(Mathf.Cos(angle) * radius, .1f, Mathf.Sin(angle) * radius));
            }
            if (kind == SpecialBlock.Yellow)
            {
                var light = go.AddComponent<Light>();
                light.type = LightType.Point; light.color = Color.yellow; light.intensity = 3; light.range = radius * 2;
                go.transform.position += Vector3.up * 2;
            }
            areas.Add(new Area { kind = kind, position = point, radius = radius, until = Time.time + seconds, visual = go });
        }
        Material effectMaterial;
        Material EffectMaterial => effectMaterial != null ? effectMaterial : effectMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        void OnDestroy() { if (effectMaterial != null) Destroy(effectMaterial); }
        void PlaceBomb(Vector3 position, int level)
        {
            var go = Placeholder.Make(PrimitiveType.Cube, Color.red, "Bomba roja", transform, position + Vector3.up * .3f, Vector3.one * .6f, false, 1);
            bombs.Add(new Bomb { position = position, until = Time.time + BlockRules.BombFuse, level = level, visual = go });
        }
        public void Explode(Vector3 position, int level, bool helper)
        {
            Explosions++;
            float radius = BlockRules.Radius(SpecialBlock.Red, level);
            foreach (var crystal in BreakableCrystal.All.ToArray())
                if (crystal != null && Vector3.Distance(crystal.transform.position, position) <= radius)
                    crystal.TakeDamage(BlockRules.RedDamage(level, helper));
            AddArea(SpecialBlock.Red, position, radius, .25f);
        }
        public bool SpawnHelper(Vector3 position)
        {
            if (!Ready || HelperCount >= BlockRules.MaxHelpers) return false;
            int level = Level(SpecialBlock.Celeste);
            var root = new GameObject("Ayudante celeste");
            root.transform.SetParent(transform);
            root.transform.position = position;
            Placeholder.Make(PrimitiveType.Capsule, BlockRules.ColorOf(SpecialBlock.Celeste), "Body", root.transform, position + Vector3.up * .55f, new Vector3(.35f, .35f, .35f), false, .3f);
            Placeholder.Make(PrimitiveType.Sphere, Color.white, "Head", root.transform, position + Vector3.up * 1.02f, Vector3.one * .3f, false, .2f);
            var helper = root.AddComponent<BlockHelper>();
            helper.owner = this; helper.expiresAt = Time.time + BlockRules.Duration(SpecialBlock.Celeste, level);
            helpers.Add(helper);
            return true;
        }
        public void ResetLab()
        {
            if (!Ready) return;
            foreach (var area in areas) Destroy(area.visual);
            foreach (var bomb in bombs) Destroy(bomb.visual);
            foreach (var helper in helpers) if (helper != null) Destroy(helper.gameObject);
            areas.Clear(); bombs.Clear(); helpers.Clear();
            Array.Clear(cooldowns, 0, cooldowns.Length);
            GreenHits = CelesteHits = 0;
            buffUntil = 0;
            Lab.RestoreCrystals();
        }
    }
}
