using UnityEngine;
using UnityEngine.InputSystem;

namespace SoulBlocks
{
    [RequireComponent(typeof(Camera))]
    public sealed class SoulCamera : MonoBehaviour
    {
        public Transform player;
        public float orbitSpeed = .16f;
        public float distance = 36f;
        public Vector3 focusOffset = new Vector3(-6, 1, -3);
        public float pitch = 42f;
        public float yaw = 45f;
        public float smoothing = 8f;
        public bool readInput = true;
        void Start() { Position(1f); }
        public void SnapToPlayer() { Position(1f); }
        void LateUpdate()
        {
            var mouse = Mouse.current;
            if (readInput && Application.isFocused && mouse != null)
            {
                if (mouse.rightButton.isPressed)
                {
                    var delta = mouse.delta.ReadValue();
                    yaw += delta.x * orbitSpeed;
                    pitch = Mathf.Clamp(pitch - delta.y * orbitSpeed, 20f, 75f);
                }
                distance = Mathf.Clamp(distance - mouse.scroll.ReadValue().y * .015f, 10f, 45f);
            }
            Position(1f - Mathf.Exp(-smoothing * Time.deltaTime));
        }
        void Position(float blend)
        {
            if (player == null) return;
            var rotation = Quaternion.Euler(pitch, yaw, 0);
            var focus = player.position + focusOffset;
            transform.SetPositionAndRotation(Vector3.Lerp(transform.position, focus - rotation * Vector3.forward * distance, blend), rotation);
        }
    }
}
