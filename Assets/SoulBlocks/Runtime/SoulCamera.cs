using UnityEngine;
using UnityEngine.InputSystem;

namespace SoulBlocks
{
    // Third-person orbit camera. It turns only while the player drags with the right mouse button
    // (every rig that swung by itself was dizzying), so the cursor stays free for the left-click
    // Rayhit aim. Position is rigid, so it stops the instant the player stops; WASD follows its
    // yaw (SoulPlayer). The wheel zooms. Field names differ from the old rigs so the scene's
    // stale serialized values do not apply.
    [RequireComponent(typeof(Camera))]
    public sealed class SoulCamera : MonoBehaviour
    {
        public Transform player;
        public float orbitYaw = 0f; // starts north-up, same as MapScreen
        public float orbitPitch = 20f;
        public float minPitch = 5f, maxPitch = 65f;
        public float orbitSensitivity = .2f; // degrees per pixel dragged
        public float orbitDistance = 7f;
        public float minDistance = 3f, maxDistance = 14f;
        public float lookHeight = 1.5f;
        public bool readInput = true;

        void Start() => SnapToPlayer();

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
            if (readInput && Application.isFocused && mouse != null)
                orbitDistance = Mathf.Clamp(orbitDistance - mouse.scroll.ReadValue().y * .01f, minDistance, maxDistance);
            if (readInput && Keys.OrbitHeld)
            {
                Vector2 drag = mouse.delta.ReadValue() * orbitSensitivity;
                orbitYaw += drag.x;
                orbitPitch = Mathf.Clamp(orbitPitch - drag.y, minPitch, maxPitch);
            }
            SnapToPlayer();
        }
    }
}
