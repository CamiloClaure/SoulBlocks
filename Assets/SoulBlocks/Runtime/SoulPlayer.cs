using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SoulBlocks
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class SoulPlayer : MonoBehaviour
    {
        public Transform visual;
        public Animator animator;
        public Camera viewCamera;
        public float moveSpeed = 6f;
        public bool readInput = true;
        public float Yaw { get; private set; } // facing (last move direction); the dash goes this way
        public float SpeedMultiplier = 1f;
        public bool MovementLocked;
        public float MovementSpeed { get; private set; }
        public bool HasMovementInput { get; private set; }
        CrystalMiningAttack mining;
        public bool IsMining => mining != null && mining.DirectionLocked;
        CharacterController controller;
        float fallingSpeed;
        int animationState;
        static readonly int Idle = Animator.StringToHash("Base Layer.Idle");
        static readonly int Walk = Animator.StringToHash("Base Layer.Walk");
        // The Spirit Path wizard (Blender FBX with Legacy Idle/Walk clips, see
        // SpiritPathWizardImport) is the default look; the classic model stays under `visual`,
        // hidden, and F2 swaps between them.
        const string WizardPath = "Characters/TheSpiritPath_Wizard_Idle_Walk";
        GameObject classicModel, wizardModel;
        Animation wizardAnimation;
        Transform wizardUpperArm, wizardForearm, wizardHand, classicHand, wizardLeftHand;
        string wizardIdle, wizardWalk;

        void Awake() { controller = GetComponent<CharacterController>(); }
        void Start()
        {
            mining = GetComponent<CrystalMiningAttack>();
            if (visual != null) visual.localPosition = Vector3.down * (controller.skinWidth + .19f);
            Yaw = visual != null ? visual.eulerAngles.y : transform.eulerAngles.y;
            BuildWizardHat(); // needs the classic rig's animator active, before the wizard hides it
            BuildSpiritPathWizard();
        }
        void Update()
        {
            if (Keys.ModelSwapPressed && wizardModel != null) ShowWizard(!wizardModel.activeSelf);
            var k = Keyboard.current;
            Vector2 input = Vector2.zero;
            if (readInput && Application.isFocused && k != null)
                input = new Vector2((k.dKey.isPressed || k.rightArrowKey.isPressed ? 1 : 0) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1 : 0),
                    (k.wKey.isPressed || k.upArrowKey.isPressed ? 1 : 0) - (k.sKey.isPressed || k.downArrowKey.isPressed ? 1 : 0));
            StepMovement(input, Time.deltaTime);
        }
        public void StepMovement(Vector2 input, float deltaTime)
        {
            HasMovementInput = !MovementLocked && input.sqrMagnitude >= .0001f;
            if (deltaTime <= 0) return;
            Vector3 before = transform.position;
            if (!MovementLocked)
            {
                // WASD relative to the camera yaw: W runs away from the camera, wherever it was turned.
                input = Vector2.ClampMagnitude(input, 1);
                if (input.sqrMagnitude >= .0001f)
                {
                    // While mining, strafe relative to the current camera-directed attack.
                    float basisYaw = IsMining ? Yaw : viewCamera != null ? viewCamera.transform.eulerAngles.y : Yaw;
                    Vector3 direction = Quaternion.Euler(0f, basisYaw, 0f) * new Vector3(input.x, 0f, input.y);
                    if (!IsMining) Yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                    TryMoveSupported(direction * moveSpeed * SpeedMultiplier * deltaTime);
                }
            }
            fallingSpeed = controller.isGrounded ? -2f : Mathf.Max(-30f, fallingSpeed - 20f * deltaTime);
            controller.Move(Vector3.up * fallingSpeed * deltaTime);
            Vector3 planar = Vector3.ProjectOnPlane(transform.position - before, Vector3.up);
            MovementSpeed = planar.magnitude / deltaTime;
            if (visual != null && !IsMining) visual.rotation = Quaternion.RotateTowards(visual.rotation, Quaternion.Euler(0f, Yaw, 0f), 1080f * deltaTime);
            int next = MovementSpeed > .08f ? Walk : Idle;
            if (animationState != next)
            {
                if (animator != null && animator.isActiveAndEnabled) animator.CrossFadeInFixedTime(next, .14f);
                if (wizardAnimation != null && wizardAnimation.isActiveAndEnabled) wizardAnimation.CrossFade(next == Walk ? wizardWalk : wizardIdle, .14f);
                animationState = next;
            }
        }
        public void FaceDirection(Vector3 direction)
        {
            direction.y = 0;
            if (direction.sqrMagnitude < .0001f) return;
            Yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            if (visual != null) visual.rotation = Quaternion.Euler(0, Yaw, 0);
        }
        // Sweeps delta in short supported steps to keep walking on the only island, sliding
        // along an edge when the full step would walk off it. Returns false if any step
        // needed that fallback (so the caller knows the full delta did not land).
        public bool TryMoveSupported(Vector3 delta)
        {
            bool full = true;
            int steps = Mathf.Max(1, Mathf.CeilToInt(delta.magnitude / .1f));
            Vector3 step = delta / steps;
            for (int i = 0; i < steps; i++)
            {
                if (Supported(transform.position + step)) { controller.Move(step); continue; }
                full = false;
                Vector3 x = new Vector3(step.x, 0, 0), z = new Vector3(0, 0, step.z);
                if (Supported(transform.position + x)) controller.Move(x);
                if (Supported(transform.position + z)) controller.Move(z);
            }
            return full;
        }
        public void MoveUnchecked(Vector3 delta) => controller.Move(delta);
        public void Teleport(Vector3 position)
        {
            controller.enabled = false;
            transform.position = position;
            controller.enabled = true;
            Physics.SyncTransforms();
        }
        public static bool IsSupportedAt(Vector3 point) => Supported(point);
        static bool Supported(Vector3 point) => Physics.Raycast(point + Vector3.up, Vector3.down, 3f, (1 << 8) | (1 << 9), QueryTriggerInteraction.Ignore);
        void BuildSpiritPathWizard()
        {
            var prefab = Resources.Load<GameObject>(WizardPath);
            if (prefab == null || visual == null || animator == null) return;
            classicModel = animator.gameObject;
            wizardModel = Instantiate(prefab, visual);
            wizardModel.name = "Spirit Path wizard";
            wizardModel.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // the Blender export faces -Z
            // Same height and feet line as the classic model, whatever units the FBX came in.
            Bounds classic = Encapsulate(classicModel.GetComponentsInChildren<SkinnedMeshRenderer>()), wizard = Encapsulate(wizardModel.GetComponentsInChildren<Renderer>());
            if (wizard.size.y > .001f) wizardModel.transform.localScale *= classic.size.y / wizard.size.y;
            wizard = Encapsulate(wizardModel.GetComponentsInChildren<Renderer>());
            wizardModel.transform.position += Vector3.up * (classic.min.y - wizard.min.y);
            wizardAnimation = wizardModel.GetComponent<Animation>();
            if (wizardAnimation != null)
            {
                wizardAnimation.cullingType = AnimationCullingType.AlwaysAnimate; // like the classic Animator
                foreach (AnimationState state in wizardAnimation)
                {
                    state.wrapMode = WrapMode.Loop;
                    if (state.name.EndsWith("Idle")) wizardIdle = state.name;
                    else if (state.name.EndsWith("Walk")) wizardWalk = state.name;
                }
            }
            foreach (var bone in wizardModel.GetComponentsInChildren<Transform>(true))
                if (bone.name == "UpperArm.R") wizardUpperArm = bone;
                else if (bone.name == "ForeArms.R") wizardForearm = bone;
                else if (bone.name == "Hand.R") wizardHand = bone;
                else if (bone.name == "Hand.L") wizardLeftHand = bone;
            ShowWizard(true);
        }

        void ShowWizard(bool show)
        {
            wizardModel.SetActive(show);
            classicModel.SetActive(!show);
            animationState = 0; // the shown model replays the current idle/walk next step
            // The scene hangs the mining pickaxe from the classic rig's right hand: carry it to
            // the shown model's hand, keeping its world size.
            var pick = mining != null && mining.pickaxe != null ? mining.pickaxe.transform : null;
            if (pick == null) return;
            if (classicHand == null) classicHand = pick.parent;
            pick.SetParent(show && wizardHand != null ? wizardHand : classicHand, true);
            pick.localPosition = Vector3.zero;
        }

        // Shoulder, elbow and hand bones of the shown model, posed by the mining swing
        // (CrystalMiningAttack). Null when the model has no such bones.
        public Transform TorchHand => wizardModel != null && wizardModel.activeSelf ? wizardLeftHand
            : animator != null && animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.LeftHand) : null;

        public void RightArm(out Transform upper, out Transform fore, out Transform hand)
        {
            if (wizardModel != null && wizardModel.activeSelf) { upper = wizardUpperArm; fore = wizardForearm; hand = wizardHand; return; }
            upper = fore = hand = null;
            if (animator == null || !animator.isHuman) return;
            upper = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            fore = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        }

        static Bounds Encapsulate(Renderer[] renderers)
        {
            var bounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds();
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        void BuildWizardHat()
        {
            if(animator==null)return;
            var head=animator.GetBoneTransform(HumanBodyBones.Head);
            if(head==null)return;
            Material material=null;
            for(int i=0;i<head.childCount;i++)
            {
                var child=head.GetChild(i);
                if(!child.name.StartsWith("Traveler leather hat"))continue;
                var renderer=child.GetComponent<Renderer>();
                if(renderer!=null)material=renderer.sharedMaterial;
                child.gameObject.SetActive(false);
            }
            if(material==null)foreach(var m in Resources.FindObjectsOfTypeAll<Material>())if(m.name=="M_CHR_Leather"){material=m;break;}
            var hat=new GameObject("Traveler wizard hat",typeof(MeshFilter),typeof(MeshRenderer));
            hat.transform.SetParent(head,false);
            // The rig's bones do not run at world scale, so a mesh authored in metres inherits the
            // bone's scale and grows to cover the island. Normalise against it and put the offset
            // in the same world units, so the hat is .84 m across whatever the rig does.
            var scale=head.lossyScale;var inverse=new Vector3(scale.x==0?1:1/scale.x,scale.y==0?1:1/scale.y,scale.z==0?1:1/scale.z);
            hat.transform.localScale=inverse;
            hat.transform.localPosition=Vector3.up*.12f*inverse.y;hat.transform.localRotation=Quaternion.identity;
            hat.GetComponent<MeshFilter>().sharedMesh=WizardHatMesh();
            hat.GetComponent<Renderer>().sharedMaterial=material;
        }

        static Mesh WizardHatMesh()
        {
            int sides=16;float coneRadius=.30f,coneHeight=.62f,bendRad=25f*Mathf.Deg2Rad,brimRadius=.42f,half=.015f;
            var vertices=new List<Vector3>();var triangles=new List<int>();
            int baseStart=vertices.Count;
            for(int i=0;i<sides;i++){float a=i*Mathf.PI*2/sides;vertices.Add(new Vector3(Mathf.Cos(a)*coneRadius,0,Mathf.Sin(a)*coneRadius));}
            int apex=vertices.Count;vertices.Add(new Vector3(0,coneHeight*Mathf.Cos(bendRad),-coneHeight*Mathf.Sin(bendRad)));
            for(int i=0;i<sides;i++){int a=baseStart+i,b=baseStart+(i+1)%sides;triangles.AddRange(new[]{apex,a,b,apex,b,a});}
            int topCenter=vertices.Count;vertices.Add(new Vector3(0,half,0));
            int topStart=vertices.Count;
            for(int i=0;i<sides;i++){float a=i*Mathf.PI*2/sides;vertices.Add(new Vector3(Mathf.Cos(a)*brimRadius,half,Mathf.Sin(a)*brimRadius));}
            int bottomCenter=vertices.Count;vertices.Add(new Vector3(0,-half,0));
            int bottomStart=vertices.Count;
            for(int i=0;i<sides;i++){float a=i*Mathf.PI*2/sides;vertices.Add(new Vector3(Mathf.Cos(a)*brimRadius,-half,Mathf.Sin(a)*brimRadius));}
            for(int i=0;i<sides;i++)
            {
                int t0=topStart+i,t1=topStart+(i+1)%sides,b0=bottomStart+i,b1=bottomStart+(i+1)%sides;
                triangles.AddRange(new[]{topCenter,t1,t0,topCenter,t0,t1});
                triangles.AddRange(new[]{bottomCenter,b0,b1,bottomCenter,b1,b0});
                triangles.AddRange(new[]{t0,t1,b0,t1,b1,b0,b0,b1,t0,b1,t1,t0});
            }
            var mesh=new Mesh{name="Traveler wizard hat"};
            mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);
            mesh.RecalculateNormals();mesh.RecalculateBounds();
            return mesh;
        }


    }
}


