using System.Collections.Generic;
using UnityEngine;

namespace SoulBlocks
{
    [DisallowMultipleComponent, RequireComponent(typeof(SoulPlayer))]
    public sealed class CrystalMiningAttack : MonoBehaviour
    {
        [Tooltip("Volumen de contacto, activo solamente durante el impacto del pico.")]
        public BoxCollider hitbox;
        public GameObject pickaxe;
        public float Duration => .55f;
        public bool IsSwinging { get; private set; }
        public bool DirectionLocked => IsSwinging || holdingAttack;
        public Vector3 MiningDirection => facing * Vector3.forward;
        bool holdingAttack;
        bool notifiedHit;
        Vector3 heldDirection;
        const float ImpactTime = .22f;
        const float ContactEnd = .32f;
        SoulPlayer player;
        float elapsed, flashUntil;
        Quaternion facing;
        Transform upperArm, forearm, hand;
        Quaternion upperRest, forearmRest;
        readonly HashSet<BreakableCrystal> hitThisSwing = new HashSet<BreakableCrystal>();
        readonly List<Renderer> tintedRenderers = new List<Renderer>();
        readonly List<MaterialPropertyBlock> originalBlocks = new List<MaterialPropertyBlock>();

        void Awake()
        {
            player = GetComponent<SoulPlayer>();
            if (hitbox != null) hitbox.enabled = false;
            if (pickaxe != null) pickaxe.SetActive(false);
        }

        void Update()
        {
            if (player.readInput) ProcessMiningInput(Keys.MineHeld, holdingAttack ? heldDirection : AimDirection());
            if (!IsSwinging || Time.deltaTime <= 0f) return;
            if (Keys.Blocked || player.MovementLocked) { holdingAttack = false; FinishSwing(); return; }

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
                    if (crystal != null && hitThisSwing.Add(crystal) && crystal.TakeHit())
                    {
                        FlashPlayer();
                        if (!notifiedHit)
                        {
                            notifiedHit = true;
                            SpecialBlockSystem.Instance?.NotifyMiningHit(crystal.transform.position, false);
                        }
                    }
                }
            }
            if (hitbox != null && elapsed >= ContactEnd) hitbox.enabled = false;
            if (elapsed >= Duration) FinishSwing();
        }

        void LateUpdate()
        {
            if (flashUntil > 0f && Time.time >= flashUntil) RestoreTint();
            if (!IsSwinging) return;
            // Aim bone segments in world space: imported Blender/FBX rigs may use different local axes.
            float windup = Mathf.SmoothStep(0, 1, elapsed / .13f);
            float strike = Mathf.SmoothStep(0, 1, (elapsed - .13f) / (ImpactTime - .13f));
            float recovery = Mathf.SmoothStep(0, 1, (elapsed - ContactEnd) / (Duration - ContactEnd));
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
                var ray = player.viewCamera.ScreenPointToRay(Keys.MousePosition);
                if (new Plane(Vector3.up, transform.position).Raycast(ray, out float distance))
                    return ray.GetPoint(distance) - transform.position;
            }
            return Quaternion.Euler(0, player.Yaw, 0) * Vector3.forward;
        }

        // Capture once per held click; camera turns must not redirect subsequent swings.
        public void ProcessMiningInput(bool held, Vector3 aimDirection)
        {
            if (!held || Keys.Blocked || player.MovementLocked || !isActiveAndEnabled)
            {
                holdingAttack = false;
                return;
            }
            if (!holdingAttack)
            {
                aimDirection.y = 0;
                heldDirection = aimDirection.sqrMagnitude > .001f ? aimDirection.normalized
                    : Quaternion.Euler(0, player.Yaw, 0) * Vector3.forward;
                holdingAttack = true;
            }
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
            elapsed = 0f;
            notifiedHit = false;
            hitThisSwing.Clear();
            if (player.animator != null && player.animator.isHuman)
            {
                upperArm = player.animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
                forearm = player.animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
                hand = player.animator.GetBoneTransform(HumanBodyBones.RightHand);
                if (upperArm != null) upperRest = upperArm.localRotation;
                if (forearm != null) forearmRest = forearm.localRotation;
            }
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
