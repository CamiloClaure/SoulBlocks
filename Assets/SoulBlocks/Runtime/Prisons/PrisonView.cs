using System.Collections;
using UnityEngine;

namespace SoulBlocks
{
    // Placeholder crystal prison + IMineable wrapper around PrisonRules. No collider: the player
    // walks through (ponytail: the pick targets purely by MineableRegistry + Radius, never
    // Physics, so a collider here would only ever get in the way of movement). Every blow that
    // lands chips shards off (Burst) and makes the crystal flinch.
    public sealed class PrisonView : MonoBehaviour, IMineable
    {
        struct Shape { public PrimitiveType type; public Color color; public Vector3 scale; }

        PrisonSystem system;
        RunState run;
        PrisonLayout layout;
        Transform body;
        Vector3 bodyScale;
        Color color;

        // Drawn by PrisonSystem as a Hud.WorldLabel (opaque box) while the player is close.
        public string Label { get; private set; }

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

        void OnEnable() => MineableRegistry.All.Add(this);
        void OnDisable() => MineableRegistry.All.Remove(this);

        void Build()
        {
            var shape = TierShape(layout.tier);
            body = Placeholder.Make(shape.type, shape.color, "Body", transform, transform.position, shape.scale, false, .3f).transform;
            bodyScale = shape.scale;
            color = shape.color;

            if (layout.tier == PrisonTier.Nucleo)
                Placeholder.Make(PrimitiveType.Cylinder, Color.white, "Ring", transform, transform.position, new Vector3(3.2f, .05f, 3.2f), false, .2f);
            if ((layout.props & PrisonProp.Resonant) != 0)
                Placeholder.Make(PrimitiveType.Cylinder, Color.cyan, "ResonantMarker", transform, transform.position, new Vector3(1.6f, .04f, 1.6f), false, .4f);
            if ((layout.props & PrisonProp.ExposedCore) != 0)
                Placeholder.Make(PrimitiveType.Sphere, Color.magenta, "ExposedCoreMarker", transform, transform.position, new Vector3(.3f, .3f, .3f), false, .6f);

            UpdateLabel(run.Prison(layout.id));
        }

        public PrisonForecast Forecast(int pickLevel)
        {
            var record = run.Prison(layout.id);
            return PrisonRules.Forecast(layout.tier, record, layout.props, pickLevel, CurrentMultiplier());
        }

        public int ApplyPickHit(int pickLevel)
        {
            var record = run.Prison(layout.id);
            if (record.broken) return 0;

            int required = PrisonRules.RequiredLevel(layout.tier);
            if (pickLevel < required)
            {
                Hud.Toast("requiere pico nv " + required);
                return 0;
            }

            int phasesBefore = record.phasesDone;
            var result = PrisonRules.ApplyHit(layout.tier, record, layout.props, pickLevel, CurrentMultiplier());
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
            Burst.Spawn(transform.position + Vector3.up * .5f, color, 14);

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
                StopAllCoroutines();
                StartCoroutine(Flinch());
            }
        }

        IEnumerator Flinch()
        {
            for (float t = 0f; t < .15f; t += Time.deltaTime)
            {
                body.localScale = bodyScale * (1f + .15f * Mathf.Sin(t / .15f * Mathf.PI));
                yield return null;
            }
            body.localScale = bodyScale;
        }

        void UpdateLabel(PrisonRecord record)
        {
            int remaining = PrisonRules.TotalHp(layout.tier) - record.damage;
            Label = TierSymbol(layout.tier) + "\nHP " + remaining + " fase " + record.phasesDone + "/" + PrisonRules.Phases(layout.tier);
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
