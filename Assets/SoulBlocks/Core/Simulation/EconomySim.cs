using System.Collections.Generic;
using UnityEngine;

namespace SoulBlocks
{
    // Milestones for one simulated run, in game minutes (-1 = never reached this run).
    public sealed class EconomySimResult
    {
        public int seed;
        public int expeditions;
        public bool blocked;
        public float minutesFirstPurchase = -1f;
        public float minutesRaiz1 = -1f;
        public float minutesRaiz2 = -1f;
        public float minutesRaiz3 = -1f;
        public float minutesDespertar = -1f;
        public float minutesNucleoBroken = -1f;
        public int totalSoulsCollected;
        public int totalWisdomGained;
        public int totalPrisonsBroken;
    }

    // Headless economy simulation (plan 1.1-1.10): a greedy bot plays one run for a seed over
    // the real Core rules (map, stats, prisons, progression) with no MonoBehaviour/Unity scene
    // involved, so it runs in an EditMode test or the WSL harness alike.
    //
    // ponytail: the bot commits to exactly one branch per expedition (out, act, back) instead of
    // routing through Raiz II bridges into a second branch on the same trip. Real play with
    // bridges can be a little faster than this; never slower, so it is the safe direction for
    // "no seed blocks progression".
    // ponytail: shelters (energy pause) and ground-leftover souls (overflow once cargo is full)
    // are not modelled; both are player conveniences that only make real runs faster/richer than
    // this sim, never slower.
    // ponytail: dash, magnet, blessings, anomalies, altars and Brote are the "optional
    // accelerator" systems the plan calls out (1.8) and are left out entirely, per scope.
    public static class EconomySim
    {
        struct Poi
        {
            public float distance;
            public bool isSoul;
            public SoulSpawnLayout soul;
            public PrisonLayout prison;
        }

        struct ExpeditionOutcome
        {
            public float timeSpent;
            public int soulsCollected;
            public int wisdomGained;
            public int prisonsBroken;
            public bool nucleoBroken;
            public bool cargoWasFull;
            public bool stoppedByEnergyBudget;
            public bool chargesStarved; // ran out of Rayhit charges while a reachable prison was still unbroken
            public float value;
            public int[] brokenByTier; // indexed like RunState.brokenByTier, applied by the caller only for the winning branch
        }

        static readonly string[] CoreOrder = { "raiz1", "raiz2", "raiz3", "despertar" };
        // rayhit_cargas is not one of the plan's named constraints (energy/cargo/rayhit level),
        // but a 1-charge Rayhit can only ever land one hit per trip: without it, multi-member
        // prison groups never finish in one visit and the branch that still owns them stops
        // scoring above a pure souls run, stalling wisdom forever once cargo/energy/level cap
        // out. Folding "charges" in as a fourth binding constraint is the bot being as sensible
        // as a human player idling on a stockpile of unspent souls would be.
        static readonly string[] BindingCandidates = { "carga", "energia", "rayhit_nivel", "rayhit_cargas" };

        public static EconomySimResult Run(int seed)
        {
            var layout = MapGen.Generate(BuildInput(seed));
            var cache = BuildPoiCache(layout);
            var run = new RunState { seed = seed, effectiveSeed = layout.effectiveSeed };
            var stats = new StatSheet();
            var result = new EconomySimResult { seed = seed };

            float timeSeconds = 0f;
            int staleStreak = 0;
            bool cargoWasFullLast = false, stoppedByEnergyLast = false, chargesStarvedLast = false;

            for (int exped = 0; exped < Balance.EconMaxExpeditions; exped++)
            {
                ProgressionRules.ApplyStats(run, stats);
                bool bought = ShopAtHub(run, stats, cargoWasFullLast, stoppedByEnergyLast, chargesStarvedLast, timeSeconds, result);
                if (bought) ProgressionRules.ApplyStats(run, stats);

                var outcome = RunOneExpedition(layout, cache, run, stats);
                timeSeconds += outcome.timeSpent;
                run.reserveSouls += outcome.soulsCollected; // cargo is deposited whole on return (plan 1.1 step 4)
                run.wisdom += outcome.wisdomGained;
                run.totalSoulsCollected += outcome.soulsCollected;
                run.totalPrisonsBroken += outcome.prisonsBroken;
                for (int t = 0; t < run.brokenByTier.Length; t++) run.brokenByTier[t] += outcome.brokenByTier[t];
                run.expeditionIndex = exped + 1;
                run.playTimeSeconds = timeSeconds;
                result.totalSoulsCollected += outcome.soulsCollected;
                result.totalWisdomGained += outcome.wisdomGained;
                result.totalPrisonsBroken += outcome.prisonsBroken;

                cargoWasFullLast = outcome.cargoWasFull;
                stoppedByEnergyLast = outcome.stoppedByEnergyBudget;
                chargesStarvedLast = outcome.chargesStarved;

                if (outcome.nucleoBroken)
                {
                    result.minutesNucleoBroken = timeSeconds / 60f;
                    result.expeditions = exped + 1;
                    return result;
                }

                bool progressed = outcome.soulsCollected > 0 || outcome.wisdomGained > 0 || outcome.prisonsBroken > 0 || bought;
                staleStreak = progressed ? 0 : staleStreak + 1;
                if (staleStreak >= Balance.EconBlockedStaleExpeditions)
                {
                    result.blocked = true;
                    result.expeditions = exped + 1;
                    return result;
                }
            }

            result.blocked = true;
            result.expeditions = Balance.EconMaxExpeditions;
            return result;
        }

        static MapGenInput BuildInput(int seed) => new MapGenInput
        {
            seed = seed,
            hubCenter = new Vector3(-12.3f, 0, -11.8f),
            anchors = new List<MapAnchor>
            {
                new MapAnchor { position = new Vector3(-5f, 0, 9.37f), direction = Vector3.forward },
                new MapAnchor { position = new Vector3(6.21f, 0, -12f), direction = Vector3.right },
                new MapAnchor { position = new Vector3(-30.7f, 0, -30f), direction = Vector3.left }
            }
        };

        // Distances never change after generation; only broken/requiresUnlock state does, so this
        // is computed once per seed instead of once per expedition.
        static List<Poi>[] BuildPoiCache(MapLayout layout)
        {
            var result = new List<Poi>[layout.branches.Count];
            for (int i = 0; i < result.Length; i++) result[i] = new List<Poi>();

            foreach (var soul in layout.soulSpawns)
            {
                if (soul.branchId < 0 || soul.branchId >= result.Length) continue;
                result[soul.branchId].Add(new Poi { distance = MapGen.PathDistanceAt(layout, soul.branchId, soul.position), isSoul = true, soul = soul });
            }
            foreach (var prison in layout.prisons)
            {
                if (prison.branchId < 0 || prison.branchId >= result.Length) continue;
                result[prison.branchId].Add(new Poi { distance = MapGen.PathDistanceAt(layout, prison.branchId, prison.position), prison = prison });
            }
            foreach (var list in result) list.Sort((a, b) => a.distance.CompareTo(b.distance));
            return result;
        }

        // Tries every branch (fresh Rayhit charges each time, as if freshly departed from the
        // tree), keeps the one with the highest value, reverts the prison damage the other
        // branches would have dealt (PrisonRules.ApplyHit mutates RunState.prisons in place, so
        // the losing trials are undone with a snapshot rather than a parallel read-only copy of
        // the hit math).
        static ExpeditionOutcome RunOneExpedition(MapLayout layout, List<Poi>[] cache, RunState run, StatSheet stats)
        {
            // Souls only score while they can still buy something: once carga/energia/
            // rayhit_nivel/rayhit_cargas are all maxed, a human player sitting on a dead
            // stockpile would obviously go finish off whatever prison is left rather than keep
            // "optimizing" for a currency with nothing left to spend it on. Without this, the
            // bot keeps picking the branch with the biggest soul count forever and never lands
            // the last few hits on a reachable, breakable prison (e.g. the Nucleo) once every
            // other branch's souls total happens to outscore it.
            float soulWeight = AnySoulsUpgradeAvailable(run) ? Balance.EconSoulValueWeight : 0f;

            int bestBranch = -1;
            ExpeditionOutcome best = default;
            best.value = float.NegativeInfinity;
            Dictionary<int, (int damage, int phasesDone, bool broken)> bestSnapshot = null;

            for (int b = 0; b < layout.branches.Count; b++)
            {
                var snapshot = SnapshotPrisons(layout, run, b);
                var outcome = DoWalk(layout, cache[b], run, stats, b, soulWeight);
                if (outcome.value > best.value)
                {
                    if (bestBranch >= 0) RestorePrisons(run, bestSnapshot);
                    best = outcome;
                    bestBranch = b;
                    bestSnapshot = snapshot;
                }
                else
                {
                    RestorePrisons(run, snapshot);
                }
            }
            return best;
        }

        static Dictionary<int, (int damage, int phasesDone, bool broken)> SnapshotPrisons(MapLayout layout, RunState run, int branchId)
        {
            var snapshot = new Dictionary<int, (int damage, int phasesDone, bool broken)>();
            foreach (var prison in layout.prisons)
            {
                if (prison.branchId != branchId) continue;
                var record = run.Prison(prison.id);
                snapshot[prison.id] = (record.damage, record.phasesDone, record.broken);
            }
            return snapshot;
        }

        static void RestorePrisons(RunState run, Dictionary<int, (int damage, int phasesDone, bool broken)> snapshot)
        {
            foreach (var kv in snapshot)
            {
                var record = run.Prison(kv.Key);
                record.damage = kv.Value.damage;
                record.phasesDone = kv.Value.phasesDone;
                record.broken = kv.Value.broken;
            }
        }

        // One out-and-back trip down a single branch: walk the cached points of interest in
        // order, spending energy-budget seconds on travel + Rayhit impacts + a per-soul-spawn
        // collection overhead, always keeping enough budget to get back (on foot, or free once
        // this branch's end portal is active). Mutates run.prisons for every hit actually thrown;
        // the caller reverts that for every branch except the one it keeps (see above).
        static ExpeditionOutcome DoWalk(MapLayout layout, List<Poi> pois, RunState run, StatSheet stats, int branchId, float soulValueWeight)
        {
            var outcome = new ExpeditionOutcome { brokenByTier = new int[5] };
            float moveSpeed = Mathf.Max(.01f, stats.Get(StatId.MoveSpeed));
            float energyBudget = Mathf.Max(0f, stats.Get(StatId.EnergyMax));
            int cargoCap = Mathf.Max(1, stats.GetInt(StatId.CargoCapacity));
            float soulDensity = stats.Get(StatId.SoulDensity);
            int rayLevel = stats.GetInt(StatId.RayLevel);
            int rayChargesMax = Mathf.Max(0, stats.GetInt(StatId.RayCharges));
            float rayRecharge = Mathf.Max(.0001f, stats.Get(StatId.RayRechargeSeconds));
            float raySoulsPerCharge = stats.Get(StatId.RaySoulsPerCharge);

            var branch = FindBranch(layout, branchId);
            bool hasRaiz1 = run.HasUnlock(UnlockIds.Raiz1);
            bool endBroken = branch != null && branch.endPrisonId >= 0 && run.Prison(branch.endPrisonId).broken;
            bool portalActive = PortalRules.IsActive(hasRaiz1, endBroken);

            var charges = new ChargeMeter(rayChargesMax, rayRecharge, rayChargesMax);
            charges.Configure(rayChargesMax, rayRecharge, raySoulsPerCharge);

            float elapsed = 0f, lastPos = 0f;
            int cargoSouls = 0;

            foreach (var poi in pois)
            {
                if (IsLocked(poi, run)) continue; // walking past costs nothing extra; the next stop's distance already covers the ground

                float moveTime = (poi.distance - lastPos) / moveSpeed;
                float stepCost = moveTime + (poi.isSoul ? Balance.EconCollectionOverheadSeconds : 0f);
                float returnCost = portalActive ? 0f : poi.distance / moveSpeed;
                if (elapsed + stepCost + returnCost > energyBudget) { outcome.stoppedByEnergyBudget = true; break; }

                elapsed += stepCost;
                lastPos = poi.distance;
                charges.Tick(stepCost);

                if (poi.isSoul)
                {
                    int spawned = Mathf.RoundToInt(poi.soul.baseCount * soulDensity);
                    int accepted = Cargo.Add(ref cargoSouls, spawned, cargoCap);
                    outcome.soulsCollected += accepted;
                    if (accepted < spawned) outcome.cargoWasFull = true;
                    charges.AddProgress(accepted);
                    continue;
                }

                var tier = poi.prison.tier;
                var record = run.Prison(poi.prison.id);
                if (rayLevel < PrisonRules.RequiredLevel(tier)) continue; // reached it, can't scratch it yet

                bool ranOutOfBudget = false;
                while (!record.broken)
                {
                    float returnNow = portalActive ? 0f : lastPos / moveSpeed;
                    if (elapsed + Balance.EconHitSeconds + returnNow > energyBudget) { ranOutOfBudget = true; break; }
                    if (!charges.TryConsume()) { outcome.chargesStarved = true; break; } // no charge left; finish this prison on a later expedition

                    var hit = PrisonRules.ApplyHit(tier, record, poi.prison.props, rayLevel);
                    elapsed += Balance.EconHitSeconds;
                    charges.Tick(Balance.EconHitSeconds);
                    outcome.wisdomGained += hit.fragments;
                    outcome.value += hit.damage * Balance.EconDamageValueWeight;

                    int acceptedSouls = Cargo.Add(ref cargoSouls, hit.souls, cargoCap);
                    outcome.soulsCollected += acceptedSouls;
                    if (acceptedSouls < hit.souls) outcome.cargoWasFull = true;
                    charges.AddProgress(acceptedSouls);

                    if (hit.broken)
                    {
                        outcome.prisonsBroken++;
                        outcome.brokenByTier[(int)tier]++;
                        if (tier == PrisonTier.Nucleo) outcome.nucleoBroken = true;
                        if (poi.prison.branchEnd && hasRaiz1) portalActive = true;
                    }
                }
                if (ranOutOfBudget) { outcome.stoppedByEnergyBudget = true; break; }
            }

            elapsed += portalActive ? 0f : lastPos / moveSpeed;
            outcome.timeSpent = elapsed;
            outcome.value += outcome.soulsCollected * soulValueWeight;
            return outcome;
        }

        static bool AnySoulsUpgradeAvailable(RunState run)
        {
            foreach (var id in BindingCandidates)
                if (CanLevel(id, run)) return true;
            return false;
        }

        static bool IsLocked(Poi poi, RunState run)
        {
            if (poi.isSoul) return !string.IsNullOrEmpty(poi.soul.requiresUnlock) && !run.HasUnlock(poi.soul.requiresUnlock);
            if (run.Prison(poi.prison.id).broken) return true;
            return !string.IsNullOrEmpty(poi.prison.requiresUnlock) && !run.HasUnlock(poi.prison.requiresUnlock);
        }

        static BranchLayout FindBranch(MapLayout layout, int id)
        {
            foreach (var branch in layout.branches)
                if (branch.id == id) return branch;
            return null;
        }

        // Greedy shopping at the tree: buy every affordable core node (in Wisdom-tree order),
        // then spend Souls on whichever of carga/energia/rayhit_nivel the bot most needs right
        // now, repeating until nothing more is affordable.
        static bool ShopAtHub(RunState run, StatSheet stats, bool cargoFull, bool energyBound, bool chargesStarved, float nowSeconds, EconomySimResult result)
        {
            bool boughtAny = false;
            for (int guard = 0; guard < 64; guard++)
            {
                bool boughtThisPass = false;

                foreach (var id in CoreOrder)
                {
                    if (run.UpgradeLevel(id) > 0) continue;
                    var def = UpgradeCatalog.Get(id);
                    if (ProgressionRules.WhyCannotBuy(run, def) != null) break; // nodes are ordered; stop at the first unmet one
                    ProgressionRules.Buy(run, def);
                    RecordFirstPurchase(nowSeconds, result);
                    RecordCoreMilestone(id, nowSeconds, result);
                    boughtThisPass = true;
                }

                string target = PickBindingUpgrade(run, stats, cargoFull, energyBound, chargesStarved);
                if (target != null)
                {
                    var def = UpgradeCatalog.Get(target);
                    if (ProgressionRules.WhyCannotBuy(run, def) == null)
                    {
                        ProgressionRules.Buy(run, def);
                        ProgressionRules.ApplyStats(run, stats); // keep RayLevel live for this same pass's gate check
                        RecordFirstPurchase(nowSeconds, result);
                        boughtThisPass = true;
                    }
                }

                if (!boughtThisPass) break;
                boughtAny = true;
            }
            return boughtAny;
        }

        static void RecordFirstPurchase(float nowSeconds, EconomySimResult result)
        {
            if (result.minutesFirstPurchase < 0f) result.minutesFirstPurchase = nowSeconds / 60f;
        }

        static void RecordCoreMilestone(string id, float nowSeconds, EconomySimResult result)
        {
            float minutes = nowSeconds / 60f;
            switch (id)
            {
                case "raiz1": result.minutesRaiz1 = minutes; break;
                case "raiz2": result.minutesRaiz2 = minutes; break;
                case "raiz3": result.minutesRaiz3 = minutes; break;
                case "despertar": result.minutesDespertar = minutes; break;
            }
        }

        // energy/cargo/rayhit level, the three binding constraints the plan calls out for the
        // greedy bot: whichever the last expedition actually ran into, else the next unmet core
        // gate's Rayhit level, else the cheapest of the three that is still affordable.
        static string PickBindingUpgrade(RunState run, StatSheet stats, bool cargoFull, bool energyBound, bool chargesStarved)
        {
            int neededLevel = NextGateRayLevel(run);
            if (CanLevel("rayhit_nivel", run) && stats.GetInt(StatId.RayLevel) < neededLevel) return "rayhit_nivel";
            if (chargesStarved && CanLevel("rayhit_cargas", run)) return "rayhit_cargas";
            if (cargoFull && CanLevel("carga", run)) return "carga";
            if (energyBound && CanLevel("energia", run)) return "energia";

            string best = null;
            int bestCost = int.MaxValue;
            foreach (var id in BindingCandidates)
            {
                if (!CanLevel(id, run)) continue;
                var def = UpgradeCatalog.Get(id);
                int cost = ProgressionRules.Cost(def, run.UpgradeLevel(id) + 1);
                if (cost < bestCost) { bestCost = cost; best = id; }
            }
            return best;
        }

        static bool CanLevel(string id, RunState run) => run.UpgradeLevel(id) < UpgradeCatalog.Get(id).maxLevel;

        static int NextGateRayLevel(RunState run)
        {
            if (run.UpgradeLevel("raiz1") == 0) return PrisonRules.RequiredLevel(PrisonTier.T1);
            if (run.UpgradeLevel("raiz2") == 0) return PrisonRules.RequiredLevel(PrisonTier.T2);
            if (run.UpgradeLevel("raiz3") == 0) return PrisonRules.RequiredLevel(PrisonTier.T3);
            if (run.UpgradeLevel("despertar") == 0) return PrisonRules.RequiredLevel(PrisonTier.T4);
            return PrisonRules.RequiredLevel(PrisonTier.Nucleo);
        }
    }
}
