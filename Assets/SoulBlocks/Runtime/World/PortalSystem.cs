using System.Collections.Generic;
using UnityEngine;

namespace SoulBlocks
{
    // Two-way travel once Raiz I is unlocked (section 1.7). A ring at a broken branch end
    // teleports the player back near the tree and ends the expedition as Portal; a matching pad
    // near the tree sends them back out to that branch end, where HubZone's own "left the
    // island" check starts the next expedition for free, nothing to do here for that half.
    // Also owns the one-line toasts for Raiz II/III (bridges, deep segments): their UnlockGate
    // opens them on UnlockGranted, this just announces it.
    public sealed class PortalSystem : MonoBehaviour
    {
        sealed class Entry
        {
            public PortalLayout portal;
            public BranchLayout branch;
            public GameObject ring;
            public GameObject pad;
            public Vector3 padPosition;
            public bool active;
        }

        static readonly Color PortalColor = new Color(.78f, .35f, 1f);

        readonly List<Entry> entries = new List<Entry>();
        RunState run;
        bool armed = true; // false after a teleport until the player stands outside every trigger

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() => GameRoot.Register<PortalSystem>();

        void OnEnable()
        {
            GameEvents.MapBuilt += OnMapBuilt;
            GameEvents.PrisonBroken += OnPrisonBroken;
            GameEvents.UnlockGranted += OnUnlockGranted;
        }

        void OnDisable()
        {
            GameEvents.MapBuilt -= OnMapBuilt;
            GameEvents.PrisonBroken -= OnPrisonBroken;
            GameEvents.UnlockGranted -= OnUnlockGranted;
        }

        void OnMapBuilt(MapLayout layout)
        {
            var root = GameRoot.Instance;
            if (root == null) return;
            run = root.Run;
            BuildEntries(root, layout);
            RefreshActive(false);
        }

        void BuildEntries(GameRoot root, MapLayout layout)
        {
            foreach (var e in entries)
            {
                if (e.ring != null) Destroy(e.ring);
                if (e.pad != null) Destroy(e.pad);
            }
            entries.Clear();

            var branchById = new Dictionary<int, BranchLayout>();
            foreach (var b in layout.branches) branchById[b.id] = b;

            Vector3 treePos = root.Tree != null ? root.Tree.position : layout.hubCenter;
            int count = layout.portals.Count;
            var scale = new Vector3(Balance.PortalDiameter, Balance.PortalThickness, Balance.PortalDiameter);

            for (int i = 0; i < count; i++)
            {
                var portal = layout.portals[i];
                if (!branchById.TryGetValue(portal.branchId, out var branch)) continue;

                var ring = Placeholder.Make(PrimitiveType.Cylinder, PortalColor, "Branch Portal " + portal.branchId,
                    transform, portal.position + Vector3.up * .05f, scale, false, 1.2f);
                ring.SetActive(false);

                Vector3 padPos = FindPadPosition(treePos, i, count);
                var pad = Placeholder.Make(PrimitiveType.Cylinder, PortalColor, "Portal Pad " + portal.branchId,
                    transform, padPos + Vector3.up * .05f, scale, false, 1.2f);
                pad.SetActive(false);

                entries.Add(new Entry { portal = portal, branch = branch, ring = ring, pad = pad, padPosition = padPos, active = false });
            }
        }

        // One slot per portal spread in a ring around the tree; each tries its own angle first,
        // then nudges around it looking for ground before giving up and stacking on the tree.
        static Vector3 FindPadPosition(Vector3 treePos, int index, int count)
        {
            float baseAngle = index * Mathf.PI * 2f / Mathf.Max(1, count);
            for (int attempt = 0; attempt < 8; attempt++)
            {
                float angle = baseAngle + attempt * (Mathf.PI / 8f);
                Vector3 candidate = treePos + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * Balance.PortalPadRadius;
                if (SoulPlayer.IsSupportedAt(candidate)) return candidate;
            }
            return treePos; // ponytail: no supported ring point found nearby, better on the tree than lost
        }

        void OnPrisonBroken(int prisonId, PrisonTier tier) => RefreshActive(true);

        void OnUnlockGranted(string id)
        {
            if (id == UnlockIds.Raiz1) RefreshActive(true);
            else if (id == UnlockIds.Raiz2) Hud.Toast("Se abrieron puentes entre ramas");
            else if (id == UnlockIds.Raiz3) Hud.Toast("Las ramas se extienden");
        }

        void RefreshActive(bool announce)
        {
            if (run == null) return;
            bool hasRaiz1 = run.HasUnlock(UnlockIds.Raiz1);
            foreach (var e in entries)
            {
                bool nowActive = PortalRules.IsActive(hasRaiz1, run.Prison(e.branch.endPrisonId).broken);
                if (nowActive == e.active) continue;
                e.active = nowActive;
                if (e.ring != null) e.ring.SetActive(nowActive);
                if (e.pad != null) e.pad.SetActive(nowActive);
                if (nowActive && announce) Hud.Toast("Portal activo en rama " + e.branch.id);
            }
        }

        void Update()
        {
            var root = GameRoot.Instance;
            if (root == null || root.Player == null || run == null) return;
            Vector3 pos = root.Player.transform.position;
            if (!armed)
            {
                // A time cooldown bounced a player who stood still on the arrival pad back out:
                // re-arm only once they have stepped off every portal and pad.
                foreach (var e in entries)
                    if (e.active && (WithinRadius(pos, e.portal.position) || WithinRadius(pos, e.padPosition))) return;
                armed = true;
            }
            foreach (var e in entries)
            {
                if (!e.active) continue;
                if (WithinRadius(pos, e.portal.position))
                {
                    root.Player.Teleport(e.padPosition);
                    if (run.inExpedition) ExpeditionController.Instance?.EndExpedition(ExpeditionEndReason.Portal);
                    armed = false;
                    return;
                }
                if (WithinRadius(pos, e.padPosition))
                {
                    root.Player.Teleport(e.portal.position);
                    armed = false;
                    return;
                }
            }
        }

        static bool WithinRadius(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x, dz = a.z - b.z;
            return dx * dx + dz * dz <= Balance.PortalTriggerRadius * Balance.PortalTriggerRadius;
        }
    }
}
