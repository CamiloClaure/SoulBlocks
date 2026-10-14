using UnityEngine;
using UnityEngine.InputSystem;

namespace SoulBlocks
{
    // Automatic centering waits for movement to stop, avoiding feedback with camera-relative WASD.
    [RequireComponent(typeof(Camera))]
    public sealed class SoulCamera : MonoBehaviour
    {
        public Transform player;
        [Header("Encuadre detrás del jugador")]
        public float orbitYaw;
        public float orbitPitch = 15f;
        public float minPitch = 5f, maxPitch = 55f;
        public float orbitDistance = 5.5f;
        public float minDistance = 2.5f, maxDistance = 10f;
        public float lookHeight = 1.1f;
        public float lookAhead = .65f;
        [Header("Giro y centrado")]
        public float orbitSensitivity = .2f;
        public bool autoCenter = false;
        public float manualOrbitHold = 2.5f;
        public float centerSmoothTime = .22f;
        [Header("Obstáculos")]
        public float collisionRadius = .22f;
        public float collisionPadding = .08f;
        public LayerMask obstructionMask = ~0;
        public bool readInput = true;

        SoulPlayer soulPlayer;
        float manualHoldRemaining, stationaryTime, yawVelocity;
        bool centering;
        bool controllingMouse;
        const float StopDelay = .35f;

        void Start()
        {
            if (player == null) return;
            soulPlayer = player.GetComponent<SoulPlayer>();
            // Read the visual so this does not depend on SoulPlayer.Start order.
            orbitYaw = soulPlayer != null && soulPlayer.visual != null
                ? soulPlayer.visual.eulerAngles.y : player.eulerAngles.y;
            SnapToPlayer();
        }

        public void RotateOrbit(Vector2 degrees)
        {
            orbitYaw = Mathf.Repeat(orbitYaw + degrees.x, 360f);
            orbitPitch = Mathf.Clamp(orbitPitch - degrees.y, minPitch, maxPitch);
            manualHoldRemaining = manualOrbitHold;
            centering = false;
            yawVelocity = 0f;
        }

        public void RecenterBehindPlayer()
        {
            centering = true;
            manualHoldRemaining = 0f;
            orbitPitch = 15f;
        }

        // Also used by scene tools and after teleports.
        public void SnapToPlayer()
        {
            if (player == null) return;
            orbitPitch = Mathf.Clamp(orbitPitch, minPitch, maxPitch);
            orbitDistance = Mathf.Clamp(orbitDistance, minDistance, maxDistance);
            var rotation = Quaternion.Euler(orbitPitch, orbitYaw, 0f);
            Vector3 forward = Quaternion.Euler(0, orbitYaw, 0) * Vector3.forward;
            Vector3 pivot = player.position + Vector3.up * lookHeight;
            Vector3 focus = pivot + forward * lookAhead;
            Vector3 desired = focus - rotation * Vector3.forward * orbitDistance;
            Vector3 ray = desired - pivot;
            float distance = ray.magnitude;
            if (distance > .001f)
            {
                // Ignore the player's controller and held tool, but include walls and crystals.
                foreach (var hit in Physics.SphereCastAll(pivot, collisionRadius, ray / distance,
                    distance, obstructionMask, QueryTriggerInteraction.Ignore))
                {
                    if (hit.transform == player || hit.transform.IsChildOf(player)) continue;
                    distance = Mathf.Min(distance, Mathf.Max(.05f, hit.distance - collisionPadding));
                }
                desired = pivot + ray.normalized * distance;
            }
            transform.SetPositionAndRotation(desired, Quaternion.LookRotation(focus - desired, Vector3.up));
        }

        public void StepCamera(float deltaTime, bool orbitHeld = false)
        {
            if (player == null || deltaTime <= 0f) return;
            if (soulPlayer == null) soulPlayer = player.GetComponent<SoulPlayer>();
            bool moving = soulPlayer != null && (soulPlayer.HasMovementInput || soulPlayer.MovementSpeed > .08f || soulPlayer.IsMining || soulPlayer.MovementLocked);
            stationaryTime = moving ? 0f : stationaryTime + deltaTime;
            manualHoldRemaining = Mathf.Max(0, manualHoldRemaining - deltaTime);
            if (!Keys.Blocked && !orbitHeld && (centering || (autoCenter && !moving && stationaryTime >= StopDelay && manualHoldRemaining <= 0)))
            {
                float targetYaw = soulPlayer != null ? soulPlayer.Yaw : player.eulerAngles.y;
                orbitYaw = Mathf.SmoothDampAngle(orbitYaw, targetYaw, ref yawVelocity, centerSmoothTime, Mathf.Infinity, deltaTime);
                if (Mathf.Abs(Mathf.DeltaAngle(orbitYaw, targetYaw)) < .1f) centering = false;
            }
            SnapToPlayer();
        }

        void LateUpdate()
        {
            if (player == null) return;
            bool wasControlling = controllingMouse;
            UpdateCursorState(Application.isFocused);
            bool orbitHeld = false;
            var mouse = Mouse.current;
            if (controllingMouse && wasControlling && mouse != null)
            {
                var keyboard = Keyboard.current;
                if (keyboard != null)
                {
                    float zoom = (keyboard.minusKey.isPressed || keyboard.numpadMinusKey.isPressed ? 1 : 0)
                        - (keyboard.equalsKey.isPressed || keyboard.numpadPlusKey.isPressed ? 1 : 0);
                    orbitDistance = Mathf.Clamp(orbitDistance + zoom * 4 * Time.deltaTime, minDistance, maxDistance);
                }
                Vector2 delta = mouse.delta.ReadValue();
                orbitHeld = delta.sqrMagnitude > .001f;
                if (orbitHeld) RotateOrbit(delta * orbitSensitivity);
                if (mouse.middleButton.wasPressedThisFrame) RecenterBehindPlayer();
            }
            StepCamera(Time.deltaTime, orbitHeld);
        }

        public void UpdateCursorState(bool focused)
        {
            controllingMouse = readInput && focused && !Keys.Blocked && !Keys.FreeCursorHeld;
            Cursor.lockState = controllingMouse ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !controllingMouse;
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused) ReleaseCursor();
        }

        void OnDisable() => ReleaseCursor();

        void ReleaseCursor()
        {
            controllingMouse = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
