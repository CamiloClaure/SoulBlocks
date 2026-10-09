using UnityEngine;

namespace SoulBlocks
{
    // Owns Run.plan (section 1.8): rolls it on ExpeditionPlanned, tracks the chosen objective's
    // progress, applies the one active anomaly (soul burst / weakened prison / dash current) and
    // the per-branch soul density hook, and draws the hub panel + expedition HUD line.
    public sealed class ExpeditionPlanningSystem : MonoBehaviour
    {
        GameObject anomalyMarker;
        bool corrienteOn;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() => GameRoot.Register<ExpeditionPlanningSystem>();

        static int DefaultSpawnCount(SoulSpawnLayout spawn) => spawn.baseCount;

        void OnEnable()
        {
            GameEvents.ExpeditionPlanned += OnExpeditionPlanned;
            GameEvents.ExpeditionStarted += OnExpeditionStarted;
            GameEvents.ExpeditionEnded += OnExpeditionEnded;
            GameEvents.SoulsCollected += OnSoulsCollected;
            GameEvents.PrisonDamaged += OnPrisonDamaged;
            GameEvents.ShelterEntered += OnShelterEntered;
            GameEvents.TileDiscovered += OnTileDiscovered;
            ExpeditionController.SpawnCountFor = CountForSpawn;
            PrisonSystem.DamageMultiplierFor = MultiplierFor;
            Hud.Register(600, DrawHud);
        }

        void OnDisable()
        {
            GameEvents.ExpeditionPlanned -= OnExpeditionPlanned;
            GameEvents.ExpeditionStarted -= OnExpeditionStarted;
            GameEvents.ExpeditionEnded -= OnExpeditionEnded;
            GameEvents.SoulsCollected -= OnSoulsCollected;
            GameEvents.PrisonDamaged -= OnPrisonDamaged;
            GameEvents.ShelterEntered -= OnShelterEntered;
            GameEvents.TileDiscovered -= OnTileDiscovered;
            ExpeditionController.SpawnCountFor = DefaultSpawnCount;
            PrisonSystem.DamageMultiplierFor = null;
            ClearCorriente();
            Hud.Unregister(DrawHud);
        }

        // Corriente (section 1.8): DashDistance x2 only while the player stands inside the
        // anomaly's radius, for the whole expedition it belongs to.
        void Update()
        {
            var root = GameRoot.Instance;
            if (root == null || root.Run == null || root.Stats == null || root.Player == null) return;
            var run = root.Run;
            var anomaly = run.plan != null ? run.plan.anomaly : null;

            if (!run.inExpedition || anomaly == null || anomaly.kind != "corriente")
            {
                ClearCorriente();
                return;
            }

            Vector3 p = root.Player.transform.position;
            float dx = p.x - anomaly.position.x, dz = p.z - anomaly.position.z;
            bool inside = dx * dx + dz * dz <= Balance.AnomalyCorrienteRadius * Balance.AnomalyCorrienteRadius;
            if (inside == corrienteOn) return;
            corrienteOn = inside;
            if (inside) root.Stats.SetModifier("anomaly.corriente", StatId.DashDistance, 0f, Balance.AnomalyCorrienteDashMultiplier);
            else root.Stats.ClearSource("anomaly.corriente");
            GameEvents.RaiseStatsChanged();
        }

        void ClearCorriente()
        {
            if (!corrienteOn) return;
            corrienteOn = false;
            var root = GameRoot.Instance;
            if (root == null || root.Stats == null) return;
            root.Stats.ClearSource("anomaly.corriente");
            GameEvents.RaiseStatsChanged();
        }

        void OnExpeditionPlanned()
        {
            var root = GameRoot.Instance;
            if (root == null || root.Run == null || root.Layout == null) return;
            var run = root.Run;
            int plannedIndex = run.expeditionIndex + 1;

            // Loaded save already has a plan for the expedition about to start: do not re-roll
            // it (buying upgrades in the hub since then must not retroactively change it).
            bool hasPlan = run.plan != null && run.plan.offered.Count == 3 && run.planForIndex == plannedIndex;
            if (!hasPlan)
            {
                run.plan = ExpeditionPlanner.Plan(root.Layout, run, root.Stats, plannedIndex);
                run.planForIndex = plannedIndex;
            }
            RebuildAnomalyMarker(run);
        }

        void OnExpeditionStarted(int index)
        {
            var root = GameRoot.Instance;
            if (root == null || root.Run == null) return;
            var anomaly = root.Run.plan != null ? root.Run.plan.anomaly : null;
            if (anomaly == null || anomaly.kind != "almas" || anomaly.consumed) return;
            GameEvents.RaiseSoulsReleased(anomaly.position, (int)Balance.AnomalyAlmasSouls);
            anomaly.consumed = true;
        }

        void OnExpeditionEnded(ExpeditionEndReason reason) => ClearCorriente();

        int CountForSpawn(SoulSpawnLayout spawn)
        {
            var root = GameRoot.Instance;
            if (root == null || root.Run == null || root.Layout == null) return spawn.baseCount;
            int comingIndex = root.Run.expeditionIndex + 1;
            return ExpeditionPlanner.SpawnCount(root.Layout, root.Run, root.Run.effectiveSeed, comingIndex, spawn);
        }

        float MultiplierFor(int prisonId)
        {
            var root = GameRoot.Instance;
            if (root == null || root.Run == null || !root.Run.inExpedition || root.Layout == null) return 1f;
            var anomaly = root.Run.plan != null ? root.Run.plan.anomaly : null;
            if (anomaly == null || anomaly.kind != "prision_debilitada") return 1f;

            foreach (var prison in root.Layout.prisons)
                if (prison.id == prisonId)
                    return prison.branchId == anomaly.branchId && (prison.position - anomaly.position).sqrMagnitude < .01f
                        ? Balance.AnomalyPrisionMultiplier : 1f;
            return 1f;
        }

        void RebuildAnomalyMarker(RunState run)
        {
            if (anomalyMarker != null) Destroy(anomalyMarker);
            anomalyMarker = null;
            var anomaly = run.plan != null ? run.plan.anomaly : null;
            if (anomaly == null) return;

            anomalyMarker = Placeholder.Make(PrimitiveType.Cylinder, AnomalyColor(anomaly.kind), "Anomaly " + anomaly.kind, transform,
                anomaly.position + Vector3.up * (Balance.AnomalyPillarHeight * .5f), new Vector3(.3f, Balance.AnomalyPillarHeight, .3f), false, 1.2f);
        }

        void OnSoulsCollected(int amount)
        {
            var root = GameRoot.Instance;
            if (root == null || root.Run == null || root.Player == null || root.Layout == null) return;
            var objective = ChosenObjective(root.Run, out int index);
            if (objective == null || objective.kind != "recolectar" || objective.done) return;
            if (NearestBranch(root.Layout, root.Player.transform.position) != objective.branchId) return;

            objective.progress = Mathf.Min(objective.goal, objective.progress + amount);
            if (objective.progress >= objective.goal) Complete(root.Run, objective, index);
        }

        void OnPrisonDamaged(int prisonId, int damage, int remainingHp)
        {
            var root = GameRoot.Instance;
            if (root == null || root.Run == null) return;
            var objective = ChosenObjective(root.Run, out int index);
            if (objective == null || objective.kind != "fracturar" || objective.done || objective.targetId != prisonId) return;

            objective.progress = Mathf.Max(0, objective.goal - remainingHp);
            if (remainingHp <= 0) Complete(root.Run, objective, index);
        }

        void OnShelterEntered(int id)
        {
            var root = GameRoot.Instance;
            if (root == null || root.Run == null) return;
            if (!root.Run.visitedShelterIds.Contains(id)) root.Run.visitedShelterIds.Add(id);

            var objective = ChosenObjective(root.Run, out int index);
            if (objective == null || objective.kind != "descubrir" || objective.done || objective.targetId != id) return;
            Complete(root.Run, objective, index);
        }

        void OnTileDiscovered(int tileIndex)
        {
            var root = GameRoot.Instance;
            if (root == null || root.Run == null) return;
            var objective = ChosenObjective(root.Run, out int index);
            if (objective == null || objective.kind != "descubrir" || objective.done) return;
            if (objective.targetId >= 0 || -(objective.targetId) - 1 != tileIndex) return;
            Complete(root.Run, objective, index);
        }

        static ObjectiveRecord ChosenObjective(RunState run, out int index)
        {
            index = -1;
            if (run.plan == null) return null;
            int i = run.plan.chosenObjective;
            if (i < 0 || i >= run.plan.offered.Count) return null;
            index = i;
            return run.plan.offered[i];
        }

        static void Complete(RunState run, ObjectiveRecord objective, int index)
        {
            objective.done = true;
            objective.progress = objective.goal;
            run.wisdom += objective.rewardWisdom;
            run.reserveSouls += objective.rewardSouls;
            GameEvents.RaiseWisdomChanged(run.wisdom);
            Hud.Toast("Objetivo cumplido: +" + objective.rewardWisdom + " Wisdom, +" + objective.rewardSouls + " almas");
            GameEvents.RaiseObjectiveCompleted(index);
        }

        static int NearestBranch(MapLayout layout, Vector3 position)
        {
            float best = float.MaxValue;
            int branch = -1;
            foreach (var tile in layout.tiles)
            {
                float dx = tile.position.x - position.x, dz = tile.position.z - position.z;
                float d2 = dx * dx + dz * dz;
                if (d2 < best) { best = d2; branch = tile.branchId; }
            }
            return branch;
        }

        void DrawHud()
        {
            var root = GameRoot.Instance;
            if (root == null || root.Run == null || root.Layout == null || root.Run.plan == null) return;
            var run = root.Run;

            if (run.inExpedition) { DrawExpeditionLine(run); return; }

            var hubZone = root.GetComponent<HubZone>();
            if (hubZone == null || !hubZone.InHub) return;
            DrawHubPanel(root, run);
        }

        void DrawHubPanel(GameRoot root, RunState run)
        {
            const float x = 320f, w = 380f;
            float y = 10f;
            GUI.Label(new Rect(x, y, w, 20), "Próxima expedición");
            y += 22f;

            for (int i = 0; i < run.plan.offered.Count; i++)
            {
                var objective = run.plan.offered[i];
                string text = Describe(root.Layout, objective) + "  (+" + objective.rewardWisdom + " Wisdom, +" + objective.rewardSouls + " almas)";
                GUI.Label(new Rect(x, y, w - 90f, 20f), text);
                if (run.plan.chosenObjective == i) GUI.Label(new Rect(x + w - 88f, y, 86f, 20f), "Elegido");
                else if (GUI.Button(new Rect(x + w - 88f, y, 86f, 20f), "Elegir")) run.plan.chosenObjective = i;
                y += 22f;
            }

            y += 4f;
            GUI.Label(new Rect(x, y, w, 20f), "Anomalía: " + (run.plan.anomaly != null ? AnomalyText(run.plan.anomaly) : "ninguna"));
            y += 22f;

            if (run.plan.hintBranch >= 0 && run.plan.hintBranch < root.Layout.branches.Count)
                GUI.Label(new Rect(x, y, w, 20f), "Rama más activa: " + SpecialtyName(root.Layout.branches[run.plan.hintBranch].specialty));
        }

        void DrawExpeditionLine(RunState run)
        {
            var objective = ChosenObjective(run, out _);
            if (objective == null) return;
            string state = objective.done ? "completado" : objective.progress + "/" + objective.goal;
            GUI.Label(new Rect(320f, 10f, 420f, 20f), "Objetivo: " + DescribeShort(objective) + " (" + state + ")");
        }

        static string SpecialtyName(BranchSpecialty specialty)
        {
            switch (specialty)
            {
                case BranchSpecialty.Souls: return "Almas";
                case BranchSpecialty.Prisons: return "Prisiones";
                default: return "Exploración";
            }
        }

        static string BranchName(MapLayout layout, int branchId)
        {
            foreach (var branch in layout.branches)
                if (branch.id == branchId) return SpecialtyName(branch.specialty);
            return "?";
        }

        static string Describe(MapLayout layout, ObjectiveRecord objective)
        {
            switch (objective.kind)
            {
                case "recolectar": return "Recolectar " + objective.goal + " almas en la rama " + BranchName(layout, objective.branchId);
                case "fracturar": return "Fracturar una prisión en la rama " + BranchName(layout, objective.branchId);
                case "descubrir": return objective.targetId >= 0
                    ? "Descubrir un refugio en la rama " + BranchName(layout, objective.branchId)
                    : "Descubrir una conexión cerca de la rama " + BranchName(layout, objective.branchId);
                default: return objective.kind;
            }
        }

        static string DescribeShort(ObjectiveRecord objective)
        {
            switch (objective.kind)
            {
                case "recolectar": return "recolectar almas";
                case "fracturar": return "fracturar prisión";
                case "descubrir": return "descubrir";
                default: return objective.kind;
            }
        }

        static string AnomalyText(AnomalyRecord anomaly)
        {
            switch (anomaly.kind)
            {
                case "almas": return "concentración de almas";
                case "prision_debilitada": return "prisión debilitada (x2 daño)";
                case "corriente": return "corriente (dash x2 cerca)";
                default: return anomaly.kind;
            }
        }

        static Color AnomalyColor(string kind)
        {
            switch (kind)
            {
                case "almas": return new Color(.6f, .2f, .9f);
                case "prision_debilitada": return new Color(1f, .5f, .1f);
                case "corriente": return new Color(.2f, .9f, 1f);
                default: return Color.white;
            }
        }
    }
}
