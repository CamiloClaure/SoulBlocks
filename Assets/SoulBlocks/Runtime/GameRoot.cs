using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SoulBlocks
{
    // The one object every self-registered system attaches itself to. Boot() creates it lazily
    // the first time anyone calls Attach<T>(), so there is no central list of systems anywhere.
    public sealed class GameRoot : MonoBehaviour
    {
        public static GameRoot Instance { get; private set; }
        public static int? PendingSeed;
        public static RunState PendingLoad;

        public RunState Run;
        public StatSheet Stats;
        public MapLayout Layout;
        public SoulPlayer Player;
        public Transform Tree;
        public Transform Island;
        public Camera ViewCamera;

        public static bool ShouldBoot => SceneManager.GetActiveScene().name == "SoulBlocks";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { if (ShouldBoot) Attach<GameRoot>(); }

        public static T Attach<T>() where T : Component
        {
            if (Instance == null)
            {
                var existing = GameObject.Find("Game Root");
                var root = existing != null ? existing : new GameObject("Game Root");
                if (root.GetComponent<GameRoot>() == null) root.AddComponent<GameRoot>(); // Awake() sets Instance
            }
            var go = Instance.gameObject;
            var component = go.GetComponent<T>();
            return component != null ? component : go.AddComponent<T>();
        }

        // Sets the pending seed/save and reloads the active scene; OnSceneLoaded rebuilds the world.
        public static void Restart(int? seed, RunState load)
        {
            PendingSeed = seed;
            PendingLoad = load;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        // First boot: BuildWorld runs from here, because sceneLoaded for the very first scene
        // fired before Awake had a chance to subscribe to it.
        void Start() => BuildWorld();

        // Every later scene reload (Restart): sceneLoaded fires normally since the subscription
        // already happened on the first Awake, and GameRoot itself survives via DontDestroyOnLoad.
        void OnSceneLoaded(Scene scene, LoadSceneMode mode) => BuildWorld();

        void BuildWorld()
        {
            Player = FindAnyObjectByType<SoulPlayer>();
            var treeRoot = GameObject.Find("Wisdom Tree");
            Tree = treeRoot != null ? treeRoot.transform : null;
            var islandRoot = GameObject.Find("Main Island");
            Island = islandRoot != null ? islandRoot.transform : null;
            ViewCamera = Player != null && Player.viewCamera != null ? Player.viewCamera : Camera.main;

            Run = PendingLoad != null ? PendingLoad : new RunState { seed = PendingSeed ?? Environment.TickCount };
            PendingLoad = null;
            PendingSeed = null;
            Stats = new StatSheet();

            var input = new MapGenInput { seed = Run.seed, hubCenter = Island != null ? Island.position : Vector3.zero };
            if (Island != null)
            {
                Physics.SyncTransforms(); // bounds below must reflect the island's authored transforms
                input.anchors = DetectAnchors(Island);
            }
            Layout = MapGen.Generate(input);
            Run.effectiveSeed = Layout.effectiveSeed;
            Run.energy = Balance.EnergyMax;

            Attach<MapBuilder>().Build(Layout, Run);
            GameEvents.RaiseStatsChanged();
            GameEvents.RaiseMapBuilt(Layout);
        }

        // For other crews' PlayMode tests: a self-contained GameRoot with a given layout/run,
        // skipping island-edge detection entirely.
        public static GameRoot BootWith(MapLayout layout, RunState run)
        {
            var instance = Attach<GameRoot>();
            instance.Layout = layout;
            instance.Run = run;
            instance.Stats = new StatSheet();
            instance.Player = FindAnyObjectByType<SoulPlayer>();
            var treeRoot = GameObject.Find("Wisdom Tree");
            instance.Tree = treeRoot != null ? treeRoot.transform : null;
            var islandRoot = GameObject.Find("Main Island");
            instance.Island = islandRoot != null ? islandRoot.transform : null;
            instance.ViewCamera = instance.Player != null && instance.Player.viewCamera != null ? instance.Player.viewCamera : Camera.main;
            Attach<MapBuilder>().Build(layout, run);
            GameEvents.RaiseStatsChanged();
            GameEvents.RaiseMapBuilt(layout);
            return instance;
        }

        // Anchors from the island's own edge: same geometry the retired block mechanic used
        // (git show archive/block-mechanic:Assets/SoulBlocks/Editor/BlockMechanicSetup.cs, BuildRoad).
        // North +Z at x=-5, East +X at z=-12, West -X at z=-30.
        static List<MapAnchor> DetectAnchors(Transform island)
        {
            var colliders = island.GetComponentsInChildren<Collider>().Where(c => c.enabled && !c.isTrigger).ToArray();
            return new List<MapAnchor>
            {
                DetectAnchor(colliders, Vector3.forward, -5f),
                DetectAnchor(colliders, Vector3.right, -12f),
                DetectAnchor(colliders, Vector3.left, -30f)
            };
        }

        static MapAnchor DetectAnchor(Collider[] ground, Vector3 direction, float crossCoordinate)
        {
            bool alongX = Mathf.Abs(direction.x) > .5f;
            bool positive = direction.x + direction.z > 0;
            var matching = ground.Where(c => crossCoordinate >= (alongX ? c.bounds.min.z : c.bounds.min.x) + .01f &&
                crossCoordinate <= (alongX ? c.bounds.max.z : c.bounds.max.x) - .01f).ToArray();
            if (matching.Length == 0) return new MapAnchor { position = Vector3.zero, direction = direction };
            float boundary = positive ? matching.Max(c => alongX ? c.bounds.max.x : c.bounds.max.z)
                : matching.Min(c => alongX ? c.bounds.min.x : c.bounds.min.z);
            float top = matching.Max(c => c.bounds.max.y);
            // Raw edge point, not offset: MapGen already starts its first tile one tile-size out from the anchor.
            Vector3 edge = alongX ? new Vector3(boundary, top, crossCoordinate) : new Vector3(crossCoordinate, top, boundary);
            return new MapAnchor { position = edge, direction = direction };
        }
    }
}
