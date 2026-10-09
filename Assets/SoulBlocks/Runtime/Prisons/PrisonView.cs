using UnityEngine;

namespace SoulBlocks
{
    // Placeholder prison + IRayTarget wrapper around PrisonRules. No collider: the player and
    // Rayhit pass through (ponytail: targeting is purely by RayTargetRegistry + Radius, never
    // Physics, so a collider here would only ever get in the way of movement).
    public sealed class PrisonView : MonoBehaviour, IRayTarget
    {
        struct Shape { public PrimitiveType type; public Color color; public Vector3 scale; }

        PrisonSystem system;
        RunState run;
        PrisonLayout layout;
        TextMesh label;

        public int PrisonId => layout.id;
        public Vector3 Position => transform.position;
        public float Radius => Balance.PrisonRadius;

        public void Init(PrisonSystem prisonSystem, RunState runState, PrisonLayout prisonLayout)
        {
            system = prisonSystem;
            run = runState;
            layout = prisonLayout;
            Build();
        }

        void OnEnable() => RayTargetRegistry.All.Add(this);
        void OnDisable() => RayTargetRegistry.All.Remove(this);

        void Build()
        {
            var shape = TierShape(layout.tier);
            Placeholder.Make(shape.type, shape.color, "Body", transform, transform.position, shape.scale, false, .3f);

            if (layout.tier == PrisonTier.Nucleo)
                Placeholder.Make(PrimitiveType.Cylinder, Color.white, "Ring", transform, transform.position, new Vector3(3.2f, .05f, 3.2f), false, .2f);
            if ((layout.props & PrisonProp.Resonant) != 0)
                Placeholder.Make(PrimitiveType.Cylinder, Color.cyan, "ResonantMarker", transform, transform.position, new Vector3(1.6f, .04f, 1.6f), false, .4f);
            if ((layout.props & PrisonProp.ExposedCore) != 0)
                Placeholder.Make(PrimitiveType.Sphere, Color.magenta, "ExposedCoreMarker", transform, transform.position, new Vector3(.3f, .3f, .3f), false, .6f);

            label = Placeholder.Label(transform, "", new Vector3(0, 1.2f, 0), Color.white);
            UpdateLabel(run.Prison(layout.id));
        }

        public PrisonForecast Forecast(int rayLevel)
        {
            var record = run.Prison(layout.id);
            return PrisonRules.Forecast(layout.tier, record, layout.props, rayLevel, CurrentMultiplier());
        }

        public int ApplyRayHit(int rayLevel)
        {
            var record = run.Prison(layout.id);
            if (record.broken) return 0;

            int required = PrisonRules.RequiredLevel(layout.tier);
            if (rayLevel < required)
            {
                Hud.Toast("requiere Rayhit nv " + required);
                return 0;
            }

            int phasesBefore = record.phasesDone;
            var result = PrisonRules.ApplyHit(layout.tier, record, layout.props, rayLevel, CurrentMultiplier());
            Resolve(result, record, phasesBefore);

            if (result.damage > 0 && (layout.props & PrisonProp.Resonant) != 0)
                system.ApplyResonance(layout, result.damage);
            return result.damage;
        }

        // Resonance landing point: flat damage, no level gate, never re-propagates (PrisonSystem
        // only calls ApplyResonance from the directly-hit prison, never from here).
        public void ApplySecondaryDamage(int damage)
        {
            var record = run.Prison(layout.id);
            if (record.broken || damage <= 0) return;
            int phasesBefore = record.phasesDone;
            var result = PrisonRules.ApplyDamage(layout.tier, record, damage);
            Resolve(result, record, phasesBefore);
        }

        float CurrentMultiplier() => PrisonSystem.DamageMultiplierFor != null ? PrisonSystem.DamageMultiplierFor(layout.id) : 1f;

        void Resolve(HitResult result, PrisonRecord record, int phasesBefore)
        {
            if (result.damage <= 0) return;

            int remainingHp = PrisonRules.TotalHp(layout.tier) - record.damage;
            GameEvents.RaisePrisonDamaged(layout.id, result.damage, remainingHp);

            for (int phase = phasesBefore + 1; phase <= record.phasesDone; phase++)
            {
                GameEvents.RaisePrisonPhaseCompleted(layout.id, phase);
                GameEvents.RaiseSoulsReleased(transform.position, PrisonRules.SoulsPerPhase(layout.tier));
            }

            if (result.fragments > 0)
            {
                run.wisdom += result.fragments;
                GameEvents.RaiseWisdomChanged(run.wisdom);
            }

            if (result.broken)
            {
                run.brokenByTier[(int)layout.tier]++;
                run.totalPrisonsBroken++;
                GameEvents.RaisePrisonBroken(layout.id, layout.tier);
                PrisonSystem.RaiseBroken(layout, transform.position);
                Hud.Toast("Prisión " + TierSymbol(layout.tier) + " rota: +" + PrisonRules.Fragments(layout.tier) + " Wisdom");
                gameObject.SetActive(false); // ponytail: hide and leave for GC; next MapBuilt won't respawn it (Run.prisons says broken)
            }
            else
            {
                UpdateLabel(record);
            }
        }

        void UpdateLabel(PrisonRecord record)
        {
            int remaining = PrisonRules.TotalHp(layout.tier) - record.damage;
            label.text = TierSymbol(layout.tier) + "\nHP " + remaining + " fase " + record.phasesDone + "/" + PrisonRules.Phases(layout.tier);
        }

        static string TierSymbol(PrisonTier tier)
        {
            switch (tier)
            {
                case PrisonTier.T1: return "I";
                case PrisonTier.T2: return "II";
                case PrisonTier.T3: return "III";
                case PrisonTier.T4: return "IV";
                default: return "N";
            }
        }

        static Shape TierShape(PrisonTier tier)
        {
            switch (tier)
            {
                case PrisonTier.T1: return new Shape { type = PrimitiveType.Cube, color = Color.yellow, scale = new Vector3(1.2f, 1.2f, 1.2f) };
                case PrisonTier.T2: return new Shape { type = PrimitiveType.Cylinder, color = new Color(.2f, .8f, .2f), scale = new Vector3(.8f, .6f, .8f) };
                case PrisonTier.T3: return new Shape { type = PrimitiveType.Capsule, color = new Color(.2f, .4f, 1f), scale = new Vector3(.8f, .6f, .8f) };
                case PrisonTier.T4: return new Shape { type = PrimitiveType.Sphere, color = Color.red, scale = new Vector3(1.2f, 1.2f, 1.2f) };
                default: return new Shape { type = PrimitiveType.Sphere, color = Color.white, scale = new Vector3(2.4f, 2.4f, 2.4f) }; // Nucleo
            }
        }
    }
}
