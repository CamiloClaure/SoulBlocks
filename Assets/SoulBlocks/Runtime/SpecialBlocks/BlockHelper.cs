using UnityEngine;

namespace SoulBlocks
{
    public sealed class BlockHelper : MonoBehaviour
    {
        public SpecialBlockSystem owner;
        public float expiresAt;
        float hitCooldown, walked;
        int cargo;
        Vector3 lastPosition;
        void Start() => lastPosition = transform.position;

        void Update()
        {
            if (owner == null || !owner.Ready) { Destroy(gameObject); return; }
            if (Time.time >= expiresAt) { Deposit(); Destroy(gameObject); return; }
            if (Keys.Blocked || Time.deltaTime <= 0) return;
            float speed = owner.SpeedAt(transform.position);
            hitCooldown -= Time.deltaTime * speed;
            Vector3 target = owner.Lab.tree.position;
            BreakableCrystal crystal = null;
            ISoul pickup = null;
            CrystalFragment fragment = null;
            if (cargo == 0)
            {
                float nearest = 12f;
                foreach (var soul in SoulRegistry.All)
                {
                    float distance = Vector3.Distance(soul.Position, transform.position);
                    if (soul.Available && distance < nearest) { nearest = distance; pickup = soul; }
                }
                foreach (var item in FindObjectsByType<CrystalFragment>(FindObjectsSortMode.None))
                {
                    float distance = Vector3.Distance(item.transform.position, transform.position);
                    if (!item.Collected && distance < nearest) { nearest = distance; fragment = item; }
                }
                if (fragment != null) target = fragment.transform.position;
                else if (pickup != null) target = pickup.Position - Vector3.up * SoulPickup.FloatHeight;
                else
                {
                    nearest = 20f;
                    foreach (var item in BreakableCrystal.All)
                    {
                        if (item == null || !item.Available) continue;
                        float distance = Vector3.Distance(item.transform.position, transform.position);
                        if (distance < nearest) { nearest = distance; crystal = item; }
                    }
                    if (crystal != null) target = crystal.transform.position;
                }
            }
            Vector3 delta = target - transform.position;
            delta.y = 0;
            if (delta.magnitude > .85f)
            {
                var next = transform.position + delta.normalized * Mathf.Min(delta.magnitude - .8f, 3.5f * speed * Time.deltaTime);
                if (SoulPlayer.IsSupportedAt(next)) transform.position = next;
                transform.rotation = Quaternion.LookRotation(delta.normalized);
            }
            else if (cargo > 0) Deposit();
            else if (fragment != null) fragment.TryCollectAt(transform.position);
            else if (pickup != null && pickup.Available) { cargo += pickup.Value; pickup.Collect(); }
            else if (crystal != null && hitCooldown <= 0)
            {
                hitCooldown = .85f;
                if (crystal.TakeDamage(1)) owner.NotifyMiningHit(crystal.transform.position, true);
            }
            walked += Vector3.Distance(transform.position, lastPosition);
            lastPosition = transform.position;
            if (walked >= BlockRules.StepDistance) { walked %= BlockRules.StepDistance; owner.NotifyStep(transform.position); }
        }

        void Deposit()
        {
            if (cargo <= 0 || owner == null || GameRoot.Instance == null) return;
            GameRoot.Instance.Run.reserveSouls += cargo;
            GameRoot.Instance.Run.totalSoulsCollected += cargo;
            owner.DeliveredSouls += cargo;
            cargo = 0;
        }
        void OnDisable() => Deposit();
    }
}
