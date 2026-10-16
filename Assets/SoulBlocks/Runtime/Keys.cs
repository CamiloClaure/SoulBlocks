using UnityEngine;
using UnityEngine.InputSystem;

namespace SoulBlocks
{
    // Central input bindings. Every query goes false while unfocused, while Alt frees the cursor or while Blocked is set
    // (UI raises Blocked = true for the duration a modal is open).
    public static class Keys
    {
        public static bool Blocked;

        // Alt frees the cursor (SoulCamera keeps it locked for mouse look) to click HUD buttons.
        public static bool FreeCursorHeld => Keyboard.current != null && (Keyboard.current.leftAltKey.isPressed || Keyboard.current.rightAltKey.isPressed);
        static bool Allowed => Application.isFocused && !Blocked && !FreeCursorHeld
            && !SpiritDialogue.HandledInputThisFrame
            && !(TreeUpgradeAltar.Instance != null && TreeUpgradeAltar.Instance.HandledInputThisFrame)
            && !(SpecialBlockSystem.Instance != null && SpecialBlockSystem.Instance.HandledInteractionThisFrame);

        public static bool DashPressed => Allowed && Keyboard.current != null && Keyboard.current.leftShiftKey.wasPressedThisFrame;
        public static bool MagnetPressed => Allowed && Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame;
        public static bool InteractPressed => Allowed && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
        public static bool MapHeld => Allowed && Keyboard.current != null && Keyboard.current.tabKey.isPressed;
        public static bool PausePressed => Allowed && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        public static bool ModelSwapPressed => Allowed && Keyboard.current != null && Keyboard.current.f2Key.wasPressedThisFrame;
        public static bool MineHeld => Allowed && Mouse.current != null && Mouse.current.leftButton.isPressed;
        static readonly Key[] PowerKeys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5 };
        public static bool PowerPressed(int index) => Allowed && Keyboard.current != null &&
            index >= 0 && index < PowerKeys.Length && Keyboard.current[PowerKeys[index]].wasPressedThisFrame;
        // Ignores Blocked: fullscreen must toggle with any menu open.
        public static bool FullscreenPressed => Application.isFocused && Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame;
        public static Vector2 MousePosition => Cursor.lockState == CursorLockMode.Locked
            ? new Vector2(Screen.width * .5f, Screen.height * .5f)
            : Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
    }
}
