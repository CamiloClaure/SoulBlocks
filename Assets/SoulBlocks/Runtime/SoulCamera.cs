using UnityEngine;
using UnityEngine.InputSystem;

namespace SoulBlocks
{
    // Third-person camera behind the player. Position is rigid (no lag: it stops the instant the
    // player stops); the yaw swings toward the player's facing at a constant speed while the
    // player moves, so it starts at once and stops dead instead of easing out. Running back
    // toward the camera does not turn it around. No manual orbit; the wheel only zooms. Field
    // names differ from the old orbit rig so the scene's stale serialized values do not apply.
    [RequireComponent(typeof(Camera))]
    public sealed class SoulCamera : MonoBehaviour
    {
        public Transform player;
        public float followDistance = 7f;
        public float followHeight = 3f;
        public float lookHeight = 1.5f;
        public float minDistance = 4f, maxDistance = 12f;
        public float followSpeed = 300f; // degrees per second
        public float noFollowAngle = 120f; // facing this far from the view = running at the camera
        public bool readInput = true;
        SoulPlayer soulPlayer;
        float yaw;

        void Start() => SnapToPlayer();

        public void SnapToPlayer()
        {
            if (player == null) return;
            yaw = Body() != null ? soulPlayer.Yaw : player.eulerAngles.y;
            Position();
        }

        void LateUpdate()
        {
            if (player == null) return;
            var mouse = Mouse.current;
            if (readInput && Application.isFocused && mouse != null)
                followDistance = Mathf.Clamp(followDistance - mouse.scroll.ReadValue().y * .01f, minDistance, maxDistance);

            var body = Body();
            if (body != null && body.MovementSpeed > .1f && Mathf.Abs(Mathf.DeltaAngle(yaw, body.Yaw)) < noFollowAngle)
                yaw = Mathf.MoveTowardsAngle(yaw, body.Yaw, followSpeed * Time.deltaTime);
            Position();
        }

        SoulPlayer Body()
        {
            if (soulPlayer == null && player != null) soulPlayer = player.GetComponent<SoulPlayer>();
            return soulPlayer;
        }

        void Position()
        {
            // Height scales with zoom so the view angle stays the same when zooming.
            float height = followHeight * followDistance / 7f;
            transform.position = player.position + Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, height, -followDistance);
            transform.LookAt(player.position + Vector3.up * lookHeight);
        }
    }
}
