using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SoulBlocks
{
    // The pickaxe: hold left click to swing toward where the camera looks. Each swing's contact
    // window hits every crystal in the hitbox once (damage and speed from MiningBalance at the
    // pickaxe level) and every prison in reach (at the level's color stage). The pickaxe levels
    // up with crystal fragments at the altar (UpgradePickaxe).
    [DisallowMultipleComponent, RequireComponent(typeof(SoulPlayer))]
    public sealed class CrystalMiningAttack : MonoBehaviour
    {
        [Tooltip("Volumen de contacto, activo solamente durante el impacto del pico.")]
        public BoxCollider hitbox;
        public GameObject pickaxe;
        public float Duration => MiningBalance.Current.Seconds(Level);
        public int Level => GameRoot.Instance != null && GameRoot.Instance.Player == player && GameRoot.Instance.Run != null ? Mathf.Clamp(GameRoot.Instance.Run.pickaxeLevel, 1, 50) : 1;
        public int Damage => MiningBalance.Current.Damage(Level);
        float swingDuration = .55f;
        PickaxeVisual pickaxeVisual;
        public bool UpgradePickaxe()
        {
            var root = GameRoot.Instance;
            if (root == null || root.Player != player || root.Run == null || Level >= 50 || IsSwinging) return false;
            int cost = MiningBalance.Current.Cost(Level);
            if (root.Run.collectedCrystals < cost) return false;
            root.Run.collectedCrystals -= cost;
            root.Run.pickaxeLevel = Level + 1;
            pickaxeVisual?.ShowLevel(Level);
            SaveSystem.SaveNow();
            return true;
        }

        public bool IsSwinging { get; private set; }
        public bool DirectionLocked => IsSwinging || holdingAttack;
        public Vector3 MiningDirection => facing * Vector3.forward;
        bool holdingAttack;
        bool notifiedHit;
        Vector3 heldDirection;
        float ImpactTime => swingDuration * .4f;
        float ContactEnd => swingDuration * (.32f / .55f);
        SoulPlayer player;
        float elapsed, flashUntil;
        Quaternion facing;
        Transform upperArm, forearm, hand;
        Quaternion upperRest, forearmRest;
        readonly HashSet<BreakableCrystal> hitThisSwing = new HashSet<BreakableCrystal>();
        readonly HashSet<IMineable> prisonsThisSwing = new HashSet<IMineable>();
        readonly List<Renderer> tintedRenderers = new List<Renderer>();
        readonly List<MaterialPropertyBlock> originalBlocks = new List<MaterialPropertyBlock>();

        void Awake()
        {
            player = GetComponent<SoulPlayer>();
            var prefab = Resources.Load<GameObject>("Mining/Pickaxe");
            if (pickaxe != null && prefab != null)
            {
                var previous = pickaxe;
                pickaxe = Instantiate(prefab, previous.transform.parent, false);
                pickaxe.transform.localPosition = previous.transform.localPosition;
                pickaxe.transform.localRotation = previous.transform.localRotation;
                pickaxe.transform.localScale = previous.transform.localScale;
                previous.SetActive(false);
                Destroy(previous);
                pickaxeVisual = pickaxe.GetComponent<PickaxeVisual>();
            }
            if (hitbox != null) hitbox.enabled = false;
            if (pickaxe != null) pickaxe.SetActive(false);
        }

        void Update()
        {
            if (player.readInput) ProcessMiningInput(Keys.MineHeld, AimDirection());
            if (!IsSwinging || Time.deltaTime <= 0f) return;
            if (Keys.Blocked || player.MovementLocked)
            { holdingAttack = false; FinishSwing(); return; }
            hitbox.transform.SetPositionAndRotation(transform.position, facing);

            float previous = elapsed;
            elapsed += Time.deltaTime * SpecialBlockSystem.PlayerTempo;
            // Crossing the impact time is also tested on long frames, so a click cannot skip damage.
            if (hitbox != null && elapsed >= ImpactTime && previous < ContactEnd)
            {
                hitbox.enabled = true;
                Physics.SyncTransforms();
                Vector3 scale = hitbox.transform.lossyScale;
                Vector3 half = Vector3.Scale(hitbox.size * .5f, new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
                var contacts = Physics.OverlapBox(hitbox.transform.TransformPoint(hitbox.center), half,
                    hitbox.transform.rotation, ~0, QueryTriggerInteraction.Ignore);
                foreach (var contact in contacts)
                {
                    var crystal = contact.GetComponentInParent<BreakableCrystal>();
                    if (crystal != null && crystal.Available && hitThisSwing.Add(crystal))
                    {
                        SpecialBlockSystem.Instance?.TryNourishCrystal(crystal, true);
                        if (!crystal.TakeDamage(Damage)) continue;
                        Landed(crystal.transform.position);
                    }
                }
                HitPrisons(half);
            }
            if (hitbox != null && elapsed >= ContactEnd) hitbox.enabled = false;
            if (elapsed >= swingDuration) FinishSwing();
        }

        // Prisons have no collider (the player walks through them), so they are tested by
        // position: a prison whose reach sphere overlaps the hitbox takes one blow at the
        // pickaxe's color stage. With Golpe en cadena (Wisdom node or blessing) the blow that
        // breaks one jumps on to the nearest other prison within Balance.ChainRange.
        void HitPrisons(Vector3 half)
        {
            int tier = PrisonRules.PickaxeTier(Level);
            foreach (var prison in MineableRegistry.All.ToArray())
            {
                if (prisonsThisSwing.Contains(prison)) continue;
                Vector3 local = Quaternion.Inverse(hitbox.transform.rotation) * (prison.Position + Vector3.up * .5f - hitbox.transform.TransformPoint(hitbox.center));
                if (Mathf.Abs(local.x) > half.x + prison.Radius || Mathf.Abs(local.y) > half.y + prison.Radius || Mathf.Abs(local.z) > half.z + prison.Radius) continue;
                prisonsThisSwing.Add(prison);
                if (prison.ApplyPickHit(tier) <= 0) continue;
                Landed(prison.Position);
                if (ChainUnlocked && prison.Forecast(tier).Kind == ForecastKind.Broken) Chain(prison.Position, tier);
            }
        }

        static bool ChainUnlocked
        {
            get
            {
                var run = GameRoot.Instance != null ? GameRoot.Instance.Run : null;
                return run != null && (run.HasUnlock(UnlockIds.Cadena) || run.activeBlessings.Contains("pico_cadena"));
            }
        }

        static void Chain(Vector3 from, int tier)
        {
            var positions = new List<Vector3>(MineableRegistry.All.Count);
            foreach (var other in MineableRegistry.All) positions.Add(other.Position);
            int next = ChainSelection.Next(from, Balance.ChainRange, positions, new List<int>());
            if (next >= 0) MineableRegistry.All[next].ApplyPickHit(tier);
        }

        // A blow that dealt damage: the player flashes, and the first one per swing feeds the
        // special block passives.
        void Landed(Vector3 position)
        {
            FlashPlayer();
            if (notifiedHit) return;
            notifiedHit = true;
            SpecialBlockSystem.Instance?.NotifyMiningHit(position, false);
        }

        void LateUpdate()
        {
            if (flashUntil > 0f && Time.time >= flashUntil) RestoreTint();
            if (!IsSwinging) return;
            // Aim bone segments in world space: imported Blender/FBX rigs may use different local axes.
            float windup = Mathf.SmoothStep(0, 1, elapsed / (swingDuration * (.13f / .55f)));
            float strike = Mathf.SmoothStep(0, 1, (elapsed - swingDuration * (.13f / .55f)) / (ImpactTime - swingDuration * (.13f / .55f)));
            float recovery = Mathf.SmoothStep(0, 1, (elapsed - ContactEnd) / (swingDuration - ContactEnd));
            if (player.visual != null) player.visual.rotation = facing * Quaternion.Euler(Mathf.Lerp(-8 * windup, 14, strike) * (1 - recovery), 0, 0);
            if (upperArm != null && forearm != null && hand != null)
            {
                var armDirection = Vector3.Lerp(new Vector3(.35f, -.8f, .2f), new Vector3(.3f, .65f, .6f), windup);
                armDirection = Vector3.Lerp(armDirection, new Vector3(.2f, -.55f, .85f), strike);
                armDirection = Vector3.Lerp(armDirection, new Vector3(.35f, -.8f, .2f), recovery);
                upperArm.rotation = Quaternion.FromToRotation(forearm.position - upperArm.position, facing * armDirection) * upperArm.rotation;
                var handDirection = Vector3.Lerp(new Vector3(0, 1, .2f), new Vector3(0, -.45f, 1), strike);
                handDirection = Vector3.Lerp(handDirection, new Vector3(0, -.8f, .25f), recovery);
                forearm.rotation = Quaternion.FromToRotation(hand.position - forearm.position, facing * handDirection) * forearm.rotation;
            }
            if (pickaxe != null)
                pickaxe.transform.rotation = facing * Quaternion.Euler(Mathf.Lerp(-25, 110, strike) * (1 - recovery), 0, 0);
        }

        Vector3 AimDirection()
        {
            if (player.viewCamera != null)
            {
                return Vector3.ProjectOnPlane(player.viewCamera.transform.forward, Vector3.up);
            }
            return Quaternion.Euler(0, player.Yaw, 0) * Vector3.forward;
        }

        // Refresh aim throughout the held click, including the current swing.
        public void ProcessMiningInput(bool held, Vector3 aimDirection)
        {
            if (!held || Keys.Blocked || player.MovementLocked || !isActiveAndEnabled)
            {
                holdingAttack = false;
                return;
            }
            aimDirection.y = 0;
            heldDirection = aimDirection.sqrMagnitude > .001f ? aimDirection.normalized
                : Quaternion.Euler(0, player.Yaw, 0) * Vector3.forward;
            holdingAttack = true;
            facing = Quaternion.LookRotation(heldDirection);
            player.FaceDirection(heldDirection);
            if (hitbox != null) hitbox.transform.SetPositionAndRotation(transform.position, facing);
            if (!IsSwinging) TrySwing(heldDirection);
        }

        public bool TrySwing(Vector3 direction)
        {
            if (!isActiveAndEnabled || IsSwinging || Keys.Blocked || player.MovementLocked || Time.timeScale <= 0f || hitbox == null) return false;
            direction.y = 0;
            if (direction.sqrMagnitude < .001f) direction = Quaternion.Euler(0, player.Yaw, 0) * Vector3.forward;
            facing = Quaternion.LookRotation(direction.normalized);
            player.FaceDirection(direction);
            hitbox.transform.rotation = facing;
            // The box extends forward from the player's feet, not from the model's hand scale.
            hitbox.transform.position = transform.position;
            swingDuration = Duration;
            pickaxeVisual?.ShowLevel(Level);
            elapsed = 0f;
            notifiedHit = false;
            hitThisSwing.Clear();
            prisonsThisSwing.Clear();
            player.RightArm(out upperArm, out forearm, out hand);
            if (upperArm != null) upperRest = upperArm.localRotation;
            if (forearm != null) forearmRest = forearm.localRotation;
            IsSwinging = true;
            if (pickaxe != null) pickaxe.SetActive(true);
            return true;
        }

        void FlashPlayer()
        {
            if (tintedRenderers.Count == 0)
            {
                foreach (var renderer in player.GetComponentsInChildren<Renderer>())
                {
                    var original = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(original);
                    originalBlocks.Add(original);
                    tintedRenderers.Add(renderer);
                    var red = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(red);
                    red.SetColor("_BaseColor", Color.red);
                    red.SetColor("_Color", Color.red);
                    renderer.SetPropertyBlock(red);
                }
            }
            flashUntil = Time.time + BreakableCrystal.HitFlashSeconds;
        }

        void RestoreTint()
        {
            for (int i = 0; i < tintedRenderers.Count; i++)
                if (tintedRenderers[i] != null) tintedRenderers[i].SetPropertyBlock(originalBlocks[i]);
            tintedRenderers.Clear();
            originalBlocks.Clear();
            flashUntil = 0f;
        }

        void FinishSwing()
        {
            IsSwinging = false;
            if (hitbox != null) hitbox.enabled = false;
            if (pickaxe != null) pickaxe.SetActive(false);
            if (upperArm != null) upperArm.localRotation = upperRest;
            if (forearm != null) forearm.localRotation = forearmRest;
            if (player != null && player.visual != null) player.visual.rotation = Quaternion.Euler(0, player.Yaw, 0);
        }

        void OnDisable() { holdingAttack = false; FinishSwing(); RestoreTint(); }

        void OnDrawGizmosSelected()
        {
            if (hitbox == null) return;
            Gizmos.color = new Color(1, .25f, .1f, .4f);
            Gizmos.matrix = hitbox.transform.localToWorldMatrix;
            Gizmos.DrawWireCube(hitbox.center, hitbox.size);
        }
    }
}
