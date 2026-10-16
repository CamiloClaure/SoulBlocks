using System.Collections;
using UnityEngine;

namespace SoulBlocks
{
    // Shift: dashes the player along their facing. Plans a destination by sampling forward,
    // stopping before solid obstacles but allowed to land past an unsupported gap, sliding to
    // small angle deviations when the straight line finds nothing. Rides the result over
    // Balance.DashTravelTime with MovementLocked, so gravity still applies underneath it.
    public sealed class DashAbility : MonoBehaviour
    {
        static readonly float[] AngleDeviationsDeg = { 0f, 15f, -15f, 30f, -30f };
        const int ObstacleMask = ~((1 << 8) | (1 << 9)); // everything but the walkable ground layers

        ChargeMeter meter;
        int maxCharges;
        Coroutine running;

        public int Charges => meter != null ? meter.Current : 0;
        public int MaxCharges => maxCharges;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() => GameRoot.Register<DashAbility>();

        void OnEnable() { GameEvents.MapBuilt += OnMapBuilt; }
        void OnDisable() { GameEvents.MapBuilt -= OnMapBuilt; meter = null; }

        void OnMapBuilt(MapLayout layout)
        {
            var stats = GameRoot.Instance.Stats;
            maxCharges = stats.GetInt(StatId.DashCharges);
            meter = new ChargeMeter(maxCharges, stats.Get(StatId.DashRechargeSeconds));
        }

        void Update()
        {
            var root = GameRoot.Instance;
            if (root == null || root.Player == null || meter == null) return;

            maxCharges = root.Stats.GetInt(StatId.DashCharges);
            meter.Configure(maxCharges, root.Stats.Get(StatId.DashRechargeSeconds));
            meter.Tick(Time.deltaTime * SpecialBlockSystem.PlayerTempo);

            if (running == null && Keys.DashPressed) TryDash(FacingDirection(root.Player));
        }

        // The input's direction (SoulPlayer.Yaw), not the model's: the model may still be turning.
        static Vector3 FacingDirection(SoulPlayer player) => Quaternion.Euler(0f, player.Yaw, 0f) * Vector3.forward;

        public bool TryDash(Vector3 direction)
        {
            var root = GameRoot.Instance;
            if (root == null || root.Player == null || meter == null || running != null) return false;
            if (root.Player.IsMining) return false;
            direction.y = 0f;
            if (direction.sqrMagnitude < .0001f) return false;
            direction.Normalize();

            if (!TryPlanDestination(root.Player.transform.position, direction, root.Stats.Get(StatId.DashDistance), root.Player.transform, out var destination))
                return false;
            if (!meter.TryConsume()) return false;

            running = StartCoroutine(RunDash(root.Player, destination));
            GameEvents.RaiseDashUsed();
            return true;
        }

        static bool TryPlanDestination(Vector3 origin, Vector3 direction, float maxDistance, Transform ignore, out Vector3 destination)
        {
            foreach (float angle in AngleDeviationsDeg)
            {
                Vector3 candidate = Quaternion.Euler(0f, angle, 0f) * direction;
                if (TryPlanAlong(origin, candidate, maxDistance, ignore, out destination)) return true;
            }
            destination = origin;
            return false;
        }

        // The farthest supported sample up to the first solid obstacle: samples in between with
        // no support (a gap) are simply skipped over, not a reason to stop.
        static bool TryPlanAlong(Vector3 origin, Vector3 direction, float maxDistance, Transform ignore, out Vector3 destination)
        {
            destination = origin;
            bool found = false;
            int steps = Mathf.Max(1, Mathf.CeilToInt(maxDistance / Balance.HabilidadesSampleStep));
            for (int i = 1; i <= steps; i++)
            {
                float distance = Mathf.Min(maxDistance, i * Balance.HabilidadesSampleStep);
                Vector3 point = origin + direction * distance;
                if (Blocked(point + Vector3.up * .5f, ignore)) break;
                if (SoulPlayer.IsSupportedAt(point))
                { destination = point; found = true; }
                if (distance >= maxDistance) break;
            }
            return found;
        }

        // The player's own CharacterController sits inside the first samples: never an obstacle.
        static bool Blocked(Vector3 point, Transform ignore)
        {
            foreach (var hit in Physics.OverlapSphere(point, Balance.DashObstacleRadius, ObstacleMask, QueryTriggerInteraction.Ignore))
                if (ignore == null || !hit.transform.IsChildOf(ignore)) return true;
            return false;
        }

        IEnumerator RunDash(SoulPlayer player, Vector3 destination)
        {
            player.MovementLocked = true;
            Vector3 start = player.transform.position;
            float duration = Mathf.Max(.0001f, Balance.DashTravelTime);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                Vector3 target = Vector3.Lerp(start, destination, Mathf.Clamp01(elapsed / duration));
                player.MoveUnchecked(target - player.transform.position);
                yield return null;
            }
            player.MovementLocked = false;
            running = null;
        }
    }
}
