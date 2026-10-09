using System;
using System.Collections.Generic;
using UnityEngine;

namespace SoulBlocks
{
    // Owns Run.prisons, brokenByTier, totalPrisonsBroken (CONTRATOS.md cooperation rules).
    // Portals are NOT ours (og/mundo, Fase 3). Spawns a PrisonView per non-broken layout entry
    // on MapBuilt; locked entries (requiresUnlock) stay inactive until UnlockGranted.
    public sealed class PrisonSystem : MonoBehaviour
    {
        // Extension points for Fase 3 crews. DamageMultiplierFor(prisonId): null = no anomaly
        // active, otherwise the "weakened prison" style multiplier applied on top of the normal
        // hit damage. Broken fires alongside GameEvents.PrisonBroken, for the random-rewards
        // crew to get the break position without re-deriving it from the layout.
        public static Func<int, float> DamageMultiplierFor;
        public static event Action<PrisonLayout, Vector3> Broken;
        public static void RaiseBroken(PrisonLayout layout, Vector3 position) => Broken?.Invoke(layout, position);

        MapLayout layout;
        RunState run;
        Transform root;
        readonly Dictionary<int, PrisonView> views = new Dictionary<int, PrisonView>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() => GameRoot.Register<PrisonSystem>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { DamageMultiplierFor = null; Broken = null; }

        void OnEnable()
        {
            GameEvents.MapBuilt += OnMapBuilt;
            GameEvents.UnlockGranted += OnUnlockGranted;
            Hud.Register(200, DrawHud);
        }

        void OnDisable()
        {
            GameEvents.MapBuilt -= OnMapBuilt;
            GameEvents.UnlockGranted -= OnUnlockGranted;
            Hud.Unregister(DrawHud);
        }

        void OnMapBuilt(MapLayout mapLayout)
        {
            layout = mapLayout;
            run = GameRoot.Instance.Run;
            if (root != null) Destroy(root.gameObject);
            views.Clear();

            root = new GameObject("Generated Prisons").transform;
            root.SetParent(transform, false);

            foreach (var prison in layout.prisons)
            {
                if (run.Prison(prison.id).broken) continue;

                var go = new GameObject("Prison " + prison.id);
                go.transform.SetParent(root, false);
                go.transform.position = prison.position;
                var view = go.AddComponent<PrisonView>();
                view.Init(this, run, prison);
                views[prison.id] = view;

                bool locked = !string.IsNullOrEmpty(prison.requiresUnlock) && !run.HasUnlock(prison.requiresUnlock);
                go.SetActive(!locked);
            }
        }

        void OnUnlockGranted(string id)
        {
            if (layout == null) return;
            foreach (var prison in layout.prisons)
                if (prison.requiresUnlock == id && views.TryGetValue(prison.id, out var view))
                    view.gameObject.SetActive(true);
        }

        // Resonance (section 1.5): other non-broken Resonant prisons sharing groupId take
        // floor(directDamage * share) (min 1), applied as flat secondary damage. The echo never
        // loops back through here, so it can never propagate further.
        public void ApplyResonance(PrisonLayout source, int directDamage)
        {
            if (source.groupId < 0) return;
            int secondary = PrisonRules.ResonanceDamage(directDamage);
            if (secondary <= 0) return;

            foreach (var other in layout.prisons)
            {
                if (other.id == source.id || other.groupId != source.groupId) continue;
                if ((other.props & PrisonProp.Resonant) == 0) continue;
                if (views.TryGetValue(other.id, out var view)) view.ApplySecondaryDamage(secondary);
            }
        }

        void DrawHud()
        {
            if (run == null) return;
            GUI.Label(new Rect(6, 26, 300, 20), "Prisiones rotas: " + run.totalPrisonsBroken);
        }
    }
}
