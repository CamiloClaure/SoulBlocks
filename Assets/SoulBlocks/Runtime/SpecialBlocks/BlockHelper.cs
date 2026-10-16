using UnityEngine;

namespace SoulBlocks
{
    // A temporary celeste helper: walks to the nearest crystal fragment and picks it up, or else
    // to the nearest crystal and chips it (one damage every .85 s), with the green speed and the
    // red explosions of a helper hit. Souls are not its job: they only follow the torch.
    public sealed class BlockHelper : MonoBehaviour
    {
        const float FragmentReach = 12f, CrystalReach = 20f, HitInterval = .85f;

        public SpecialBlockSystem owner;
        public float expiresAt;
        float hitCooldown, walked;
        Vector3 lastPosition;

        void Start() => lastPosition = transform.position;

        void Update()
        {
            if (owner == null || !owner.Ready || Time.time >= expiresAt) { Destroy(gameObject); return; }
            if (Keys.Blocked || Time.deltaTime <= 0) return;
            float speed = owner.SpeedAt(transform.position);
            hitCooldown -= Time.deltaTime * speed;

            Vector3 target = owner.Lab.tree.position;
            CrystalFragment fragment = Nearest(FindObjectsByType<CrystalFragment>(FindObjectsSortMode.None), f => !f.Collected, FragmentReach);
            BreakableCrystal crystal = fragment == null ? Nearest(BreakableCrystal.All, c => c != null && c.Available, CrystalReach) : null;
            if (fragment != null) target = fragment.transform.position;
            else if (crystal != null) target = crystal.transform.position;

            Vector3 delta = target - transform.position;
            delta.y = 0;
            if (delta.magnitude > .85f)
            {
                var next = transform.position + delta.normalized * Mathf.Min(delta.magnitude - .8f, 3.5f * speed * Time.deltaTime);
                if (SoulPlayer.IsSupportedAt(next)) transform.position = next;
                transform.rotation = Quaternion.LookRotation(delta.normalized);
            }
            else if (fragment != null) fragment.TryCollectAt(transform.position);
            else if (crystal != null && hitCooldown <= 0)
            {
                hitCooldown = HitInterval;
                owner.TryNourishCrystal(crystal, true);
                if (crystal.TakeDamage(1)) owner.NotifyMiningHit(crystal.transform.position, true);
            }

            walked += Vector3.Distance(transform.position, lastPosition);
            lastPosition = transform.position;
            if (walked >= BlockRules.StepDistance) { walked %= BlockRules.StepDistance; owner.NotifyStep(transform.position); }
        }

        T Nearest<T>(System.Collections.Generic.IEnumerable<T> items, System.Func<T, bool> usable, float reach) where T : Component
        {
            T best = null;
            float nearest = reach;
            foreach (var item in items)
            {
                if (!usable(item)) continue;
                float distance = Vector3.Distance(item.transform.position, transform.position);
                if (distance < nearest) { nearest = distance; best = item; }
            }
            return best;
        }
    }
}
