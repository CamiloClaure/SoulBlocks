using UnityEngine;
using UnityEngine.InputSystem;

namespace SoulBlocks
{
    // The altar in front of the central tree: E raises its five symbols, then opens the special
    // block shop, which leads on to the pickaxe upgrades and the talent board.
    public sealed class TreeUpgradeAltar : MonoBehaviour
    {
        public static TreeUpgradeAltar Instance { get; private set; }
        public Transform[] symbols;
        [Min(.1f)] public float riseSeconds = .85f;
        [Min(.5f)] public float interactionRadius = 2.8f;
        public bool Opening { get; private set; }
        public float RevealProgress { get; private set; }
        Vector3[] resting;
        bool ownsLock;
        int handledFrame = -1;
        public bool HandledInputThisFrame => handledFrame == Time.frameCount;
        public void MarkInputHandled() => handledFrame = Time.frameCount;
        SoulPlayer Player => GameRoot.Instance != null ? GameRoot.Instance.Player : null;
        public bool IsNear => Player != null && Vector3.Distance(Player.transform.position, transform.position) <= interactionRadius;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Instance = null;
        void Awake()
        {
            resting = new Vector3[symbols != null ? symbols.Length : 0];
            for (int i = 0; i < resting.Length; i++)
                if (symbols[i] != null)
                    resting[i] = symbols[i].localPosition;
            ApplyPose();
        }

        void OnEnable() { Instance = this; Hud.Register(730, Draw); }
        void OnDisable()
        {
            Hud.Unregister(Draw);
            ReleaseLock();
            Opening = false;
            if (Instance == this) Instance = null;
        }

        public bool TryInteract()
        {
            var system = SpecialBlockSystem.Instance;
            if (!isActiveAndEnabled || !IsNear || Opening || Keys.Blocked || Time.timeScale <= 0 || system == null || !system.Ready) return false;
            Opening = true;
            ownsLock = true;
            MarkInputHandled();
            Keys.Blocked = true;
            Player.MovementLocked = true;
            return true;
        }

        void ReleaseLock()
        {
            if (!ownsLock) return;
            ownsLock = false;
            Keys.Blocked = false;
            if (Player != null) Player.MovementLocked = false;
        }

        void Update()
        {
            var system = SpecialBlockSystem.Instance;
            var progression = GameRoot.Instance != null ? GameRoot.Instance.GetComponent<ProgressionSystem>() : null;
            if (Player != null && Player.readInput && Keys.InteractPressed) TryInteract();
            if (Opening && (!IsNear || (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)))
            { Opening = false; ReleaseLock(); MarkInputHandled(); }
            bool shown = Opening || (system != null && system.ShopOpen) || (progression != null && progression.IsOpen);
            RevealProgress = Mathf.MoveTowards(RevealProgress, shown ? 1 : 0, Time.unscaledDeltaTime / Mathf.Max(.1f, riseSeconds));
            ApplyPose();
            if (Opening && RevealProgress >= 1)
            {
                Opening = false;
                ReleaseLock();
                if (system != null) system.OpenShop();
            }
        }

        void ApplyPose()
        {
            if (resting == null) return;
            float t = Mathf.SmoothStep(0, 1, RevealProgress);
            for (int i = 0; i < resting.Length; i++)
            {
                if (symbols[i] == null) continue;
                symbols[i].gameObject.SetActive(RevealProgress > .001f);
                symbols[i].localPosition = resting[i] + Vector3.down * (1 - t) * 1.2f;
            }
        }

        void Draw()
        {
            if (IsNear && !Keys.Blocked) Hud.WorldLabel(transform.position + Vector3.up * 1.2f, "E: altar · bloques y talentos");
        }
    }
}
