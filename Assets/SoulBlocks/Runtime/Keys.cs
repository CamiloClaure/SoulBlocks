using UnityEngine;
using UnityEngine.InputSystem;

namespace SoulBlocks
{
    // Central input bindings. Every query goes false while unfocused or while Blocked is set
    // (UI raises Blocked = true for the duration a modal is open).
    public static class Keys
    {
        public static bool Blocked;

        public static bool FreeCursorHeld => Keyboard.current != null && (Keyboard.current.leftAltKey.isPressed || Keyboard.current.rightAltKey.isPressed);
        static bool Allowed => Application.isFocused && !Blocked && !FreeCursorHeld
            && !(SpecialBlockSystem.Instance != null && SpecialBlockSystem.Instance.HandledInteractionThisFrame);

        public static bool DashPressed => Allowed && Keyboard.current != null && Keyboard.current.leftShiftKey.wasPressedThisFrame;
        public static bool MagnetPressed => Allowed && Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame;
        public static bool InteractPressed => Allowed && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
        public static bool MapHeld => Allowed && Keyboard.current != null && Keyboard.current.tabKey.isPressed;
        public static bool PausePressed => Allowed && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        public static bool MineHeld => Allowed && Mouse.current != null && Mouse.current.leftButton.isPressed;
        public static bool RayAimHeld => Allowed && Keyboard.current != null && Keyboard.current.rKey.isPressed;
        public static bool RayReleased => Allowed && Keyboard.current != null && Keyboard.current.rKey.wasReleasedThisFrame;
        public static Vector2 MousePosition => Cursor.lockState == CursorLockMode.Locked
            ? new Vector2(Screen.width * .5f, Screen.height * .5f)
            : Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
    }
}
