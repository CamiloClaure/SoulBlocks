using UnityEngine;
using UnityEngine.InputSystem;

namespace SoulBlocks
{
    // Central input bindings. Every query goes false while unfocused or while Blocked is set
    // (UI raises Blocked = true for the duration a modal is open).
    public static class Keys
    {
        public static bool Blocked;

        static bool Allowed => Application.isFocused && !Blocked;

        public static bool DashPressed => Allowed && Keyboard.current != null && Keyboard.current.leftShiftKey.wasPressedThisFrame;
        public static bool MagnetPressed => Allowed && Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame;
        public static bool InteractPressed => Allowed && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
        public static bool MapHeld => Allowed && Keyboard.current != null && Keyboard.current.tabKey.isPressed;
        public static bool PausePressed => Allowed && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        public static bool RayAimHeld => Allowed && Mouse.current != null && Mouse.current.leftButton.isPressed;
        public static bool RayReleased => Allowed && Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
        public static Vector2 MousePosition => Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
    }
}
