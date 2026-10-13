using UnityEngine;
using UnityEngine.InputSystem;

namespace SoulBlocks
{
    // Third-person orbit camera with mouse look: while playing, the cursor is locked and hidden
    // and the mouse turns the camera around the player. Holding Alt (Keys.CursorHeld) or any
    // modal (Keys.Blocked) frees the cursor for clicking. Left click stays free for the pick.
    // Position is rigid, so it stops the instant the mouse stops; WASD follows its yaw
    // (SoulPlayer). The wheel zooms ZoomStep per notch (Input System reports +-1 per notch),
    // except while a modal wants the wheel for itself. Field names differ from the old rigs so
    // the scene's stale serialized values do not apply.
    [RequireComponent(typeof(Camera))]
    public sealed class SoulCamera : MonoBehaviour
    {
        const float ZoomStep = 1.5f;

        public Transform player;
        public float orbitYaw = 0f; // starts north-up, same as MapScreen
        public float orbitPitch = 20f;
        public float minPitch = 5f, maxPitch = 65f;
        public float orbitSensitivity = .2f; // degrees per pixel of mouse movement
        public float orbitDistance = 11.5f;
        public float minDistance = 3f, maxDistance = 20f;
        public float lookHeight = 1.5f;
        public bool readInput = true;
        bool looked; // mouse look was on last frame

        void Start() => SnapToPlayer();

        void OnDisable() => FreeCursor(true);

        // ponytail: no wall/terrain collision, the camera can clip into tall props; add a SphereCast pull-in if it shows up in playtests.
        public void SnapToPlayer()
        {
            if (player == null) return;
            transform.rotation = Quaternion.Euler(orbitPitch, orbitYaw, 0f);
            transform.position = player.position + Vector3.up * lookHeight - transform.forward * orbitDistance;
        }

        void LateUpdate()
        {
            if (player == null) return;
            var mouse = Mouse.current;
            bool playing = readInput && Application.isFocused && !Keys.Blocked && mouse != null;
            bool look = playing && !Keys.CursorHeld;
            if (readInput) FreeCursor(!look);
            if (playing)
                orbitDistance = Mathf.Clamp(orbitDistance - mouse.scroll.ReadValue().y * ZoomStep, minDistance, maxDistance);
            // Skips the first frame after locking: the cursor warp to the center reads as a big delta.
            if (look && looked)
            {
                Vector2 delta = mouse.delta.ReadValue() * orbitSensitivity;
                orbitYaw += delta.x;
                orbitPitch = Mathf.Clamp(orbitPitch - delta.y, minPitch, maxPitch);
            }
            looked = look;
            SnapToPlayer();
        }

        static void FreeCursor(bool free)
        {
            Cursor.lockState = free ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = free;
        }
    }
}
