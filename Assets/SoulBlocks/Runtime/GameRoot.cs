using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SoulBlocks
{
    // The one object every self-registered system attaches to. Systems call Register<T>() from a
    // BeforeSceneLoad hook; every time the "SoulBlocks" scene loads (first play, Restart, a test
    // loading it) a fresh root is created in that scene with every registered system attached.
    // No central list of systems anywhere, no DontDestroyOnLoad.
    public sealed class GameRoot : MonoBehaviour
    {
        public const string SceneName = "SoulBlocks";
        public static GameRoot Instance { get; private set; }
        public static int? PendingSeed;
        public static RunState PendingLoad;
        static readonly List<Type> systems = new List<Type>();

        public RunState Run;
        public StatSheet Stats;
        public MapLayout Layout;
        public SoulPlayer Player;
        public Transform Tree;
        public Transform Island;
        public Camera ViewCamera;
        bool built;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Instance = null;
            systems.Clear();
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        // sceneLoaded also fires for the first scene when subscribed this early.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Hook() { SceneManager.sceneLoaded -= OnSceneLoaded; SceneManager.sceneLoaded += OnSceneLoaded; }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != SceneName || (Instance != null && Instance.gameObject.scene == scene)) return;
            var go = new GameObject("Game Root");
            SceneManager.MoveGameObjectToScene(go, scene);
            go.AddComponent<GameRoot>(); // Awake sets Instance; Start builds the world
            AttachSystems();
        }

        // Call from [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)].
        public static void Register<T>() where T : Component
        {
            if (!systems.Contains(typeof(T))) systems.Add(typeof(T));
        }

        static void AttachSystems()
        {
            foreach (var type in systems)
                if (Instance.GetComponent(type) == null) Instance.gameObject.AddComponent(type);
        }

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

        // Sets the pending seed/save and reloads the scene; OnSceneLoaded builds a fresh root.
        public static void Restart(int? seed, RunState load)
        {
            PendingSeed = seed;
            PendingLoad = load;
            SceneManager.LoadScene(SceneName);
        }

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }
        void Start() { if (!built) BuildWorld(); }

        void BuildWorld()
        {
            built = true;
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
            instance.built = true;
            AttachSystems();
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
