using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SoulBlocks
{
    // Hold R to aim (a ground-plane point under the cursor, clamped to RayLength),
    // release to fire: the player travels the clamped segment at Balance.RayhitTravelSpeed,
    // hitting every IRayTarget it crosses as it passes (a misaligned shot still hits whatever
    // is on the line). Charges recharge from time and from souls collected.
    public sealed class RayhitAbility : MonoBehaviour
    {
        ChargeMeter meter;
        int maxCharges;
        int soulsTowardCharge;
        LineRenderer line;
        Material lineMaterial;
        Vector3 lastAimPoint;
        bool lastAimValid;
        bool aimingNow;
        readonly List<int> aimedIndices = new List<int>();
        int aimedRayLevel;
        Coroutine running;

        public int Charges => meter != null ? meter.Current : 0;
        public int MaxCharges => maxCharges;
        public int SoulsTowardCharge => soulsTowardCharge;
        public int SoulsPerCharge
        {
            get
            {
                var root = GameRoot.Instance;
                return root != null ? Mathf.Max(1, root.Stats.GetInt(StatId.RaySoulsPerCharge)) : 1;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() => GameRoot.Register<RayhitAbility>();

        void OnEnable()
        {
            GameEvents.MapBuilt += OnMapBuilt;
            GameEvents.SoulsCollected += OnSoulsCollected;
            Hud.Register(301, DrawForecast);
        }

        void OnDisable()
        {
            GameEvents.MapBuilt -= OnMapBuilt;
            GameEvents.SoulsCollected -= OnSoulsCollected;
            Hud.Unregister(DrawForecast);
            meter = null;
        }

        void OnMapBuilt(MapLayout layout)
        {
            var stats = GameRoot.Instance.Stats;
            maxCharges = stats.GetInt(StatId.RayCharges);
            meter = new ChargeMeter(maxCharges, stats.Get(StatId.RayRechargeSeconds));
            soulsTowardCharge = 0;
        }

        void OnSoulsCollected(int amount)
        {
            if (meter == null) return;
            meter.AddProgress(amount);
            soulsTowardCharge = (soulsTowardCharge + amount) % SoulsPerCharge;
        }

        void Update()
        {
            var root = GameRoot.Instance;
            if (root == null || root.Player == null || meter == null) { aimingNow = false; HideLine(); return; }

            maxCharges = root.Stats.GetInt(StatId.RayCharges);
            meter.Configure(maxCharges, root.Stats.Get(StatId.RayRechargeSeconds), SoulsPerCharge);
            meter.Tick(Time.deltaTime * SpecialBlockSystem.PlayerTempo);

            if (running != null) { aimingNow = false; HideLine(); return; }
            if (root.Player.IsMining || Keys.Blocked) { lastAimValid = false; aimingNow = false; HideLine(); return; }

            if (Keys.RayAimHeld && TryComputeAimPoint(root, out var aimPoint))
            {
                lastAimPoint = aimPoint;
                lastAimValid = true;
                aimingNow = true;
                UpdateLine(root, aimPoint);
            }
            else
            {
                aimingNow = false;
                HideLine();
            }

            if (Keys.RayReleased && lastAimValid)
            {
                Fire(lastAimPoint);
                lastAimValid = false;
            }
        }

        static bool TryComputeAimPoint(GameRoot root, out Vector3 point)
        {
            point = default;
            if (root.ViewCamera == null) return false;
            var ray = root.ViewCamera.ScreenPointToRay(Keys.MousePosition);
            var plane = new Plane(Vector3.up, root.Player.transform.position);
            if (!plane.Raycast(ray, out float enter)) return false;
            point = ray.GetPoint(enter);
            return true;
        }

        static Vector3 ClampToRange(Vector3 from, Vector3 to, float maxLength)
        {
            Vector3 delta = to - from;
            float length = delta.magnitude;
            return length <= maxLength || length < .0001f ? to : from + delta / length * maxLength;
        }

        static List<(Vector3 pos, float radius)> TargetSpheres()
        {
            var list = new List<(Vector3 pos, float radius)>(RayTargetRegistry.All.Count);
            foreach (var target in RayTargetRegistry.All) list.Add((target.Position, target.Radius));
            return list;
        }

        void UpdateLine(GameRoot root, Vector3 aimPoint)
        {
            Vector3 from = root.Player.transform.position;
            Vector3 to = ClampToRange(from, aimPoint, root.Stats.Get(StatId.RayLength));
            EnsureLine();
            line.enabled = true;
            line.SetPosition(0, from);
            line.SetPosition(1, to);

            aimedRayLevel = root.Stats.GetInt(StatId.RayLevel);
            aimedIndices.Clear();
            aimedIndices.AddRange(RaySegment.Hits(from, to, TargetSpheres()));
        }

        // Hud.WorldLabel must run inside a registered draw callback, not from Update.
        void DrawForecast()
        {
            if (!aimingNow) return;
            foreach (int index in aimedIndices)
            {
                if (index < 0 || index >= RayTargetRegistry.All.Count) continue;
                var target = RayTargetRegistry.All[index];
                Hud.WorldLabel(target.Position, target.Forecast(aimedRayLevel).Describe());
            }
        }

        void EnsureLine()
        {
            if (line != null) return;
            var go = new GameObject("Rayhit Preview");
            line = go.AddComponent<LineRenderer>();
            if (lineMaterial == null)
                lineMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Rayhit Line" };
            line.material = lineMaterial;
            line.startColor = line.endColor = new Color(1f, .25f, .25f);
            line.startWidth = line.endWidth = .05f;
            line.positionCount = 2;
            line.useWorldSpace = true;
        }

        void HideLine() { if (line != null) line.enabled = false; }

        public bool Fire(Vector3 aimPoint)
        {
            var root = GameRoot.Instance;
            if (root == null || root.Player == null || meter == null || running != null) return false;
            if (root.Player.IsMining) return false;
            if (!meter.TryConsume()) return false;

            Vector3 from = root.Player.transform.position;
            Vector3 clamped = ClampToRange(from, aimPoint, root.Stats.Get(StatId.RayLength));
            Vector3 to = FarthestSupportedPoint(from, clamped);

            running = StartCoroutine(RunFire(root, from, to));
            return true;
        }

        static Vector3 FarthestSupportedPoint(Vector3 from, Vector3 to)
        {
            Vector3 best = from;
            float distance = Vector3.Distance(from, to);
            int steps = Mathf.Max(1, Mathf.CeilToInt(distance / Balance.HabilidadesSampleStep));
            for (int i = 0; i <= steps; i++)
            {
                Vector3 point = Vector3.Lerp(from, to, (float)i / steps);
                if (SoulPlayer.IsSupportedAt(point)) best = point;
            }
            return best;
        }

        IEnumerator RunFire(GameRoot root, Vector3 from, Vector3 to)
        {
            var player = root.Player;
            player.MovementLocked = true;

            int rayLevel = root.Stats.GetInt(StatId.RayLevel);
            var hitOrder = RaySegment.Hits(from, to, TargetSpheres()); // indices into RayTargetRegistry.All
            float totalDistance = Vector3.Distance(from, to);
            Vector3 direction = totalDistance > .0001f ? (to - from) / totalDistance : Vector3.zero;
            float speed = Mathf.Max(.0001f, Balance.RayhitTravelSpeed);

            Vector3 position = from;
            float traveled = 0f;
            int nextHit = 0;
            int hitsApplied = 0;

            while (traveled < totalDistance)
            {
                float step = Mathf.Min(speed * Time.deltaTime, totalDistance - traveled);
                traveled += step;
                Vector3 next = from + direction * traveled;
                player.MoveUnchecked(next - position);
                position = next;

                while (nextHit < hitOrder.Count && Vector3.Dot(RayTargetRegistry.All[hitOrder[nextHit]].Position - from, direction) <= traveled)
                {
                    hitsApplied += ApplyHit(hitOrder[nextHit], rayLevel);
                    nextHit++;
                }
                yield return null;
            }
            while (nextHit < hitOrder.Count)
                hitsApplied += ApplyHit(hitOrder[nextHit++], rayLevel);

            bool chainUnlocked = root.Run.HasUnlock(UnlockIds.Cadena) || root.Run.activeBlessings.Contains("rayhit_cadena");
            if (chainUnlocked)
            {
                var positions = new List<Vector3>(RayTargetRegistry.All.Count);
                foreach (var target in RayTargetRegistry.All) positions.Add(target.Position);
                int chainIndex = ChainSelection.Next(position, Balance.ChainRange, positions, hitOrder);
                if (chainIndex >= 0) hitsApplied += ApplyHit(chainIndex, rayLevel);
            }

            GameEvents.RaiseRayhitFired(from, position, hitsApplied);
            player.MovementLocked = false;
            running = null;
        }

        static int ApplyHit(int index, int rayLevel)
        {
            if (index < 0 || index >= RayTargetRegistry.All.Count) return 0;
            RayTargetRegistry.All[index].ApplyRayHit(rayLevel);
            return 1;
        }
    }
}
