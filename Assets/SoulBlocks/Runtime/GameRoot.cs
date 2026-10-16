using System;
using System.Collections.Generic;
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
        public const string IslandName = "Main Island";
        public static GameRoot Instance { get; private set; }
        public static RunState PendingLoad;
        static readonly List<Type> systems = new List<Type>();

        public RunState Run;
        [NonSerialized] public StatSheet Stats;
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

        // Sets the pending save (null = new game) and reloads the scene; OnSceneLoaded builds a
        // fresh root.
        public static void Restart(RunState load)
        {
            PendingLoad = load;
            SceneManager.LoadScene(SceneName);
        }

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }
        void Start() { if (!built) BuildWorld(); }

        // The world is hand-built in the scene: the layout comes from its paths and sites
        // (SceneLayout), and gated content is hidden before anyone reacts to MapBuilt.
        void BuildWorld()
        {
            built = true;
            Player = FindAnyObjectByType<SoulPlayer>();
            var tree = FindAnyObjectByType<CentralSpiritTree>();
            Tree = tree != null ? tree.transform : null;
            var island = GameObject.Find(IslandName);
            Island = island != null ? island.transform : null;
            ViewCamera = Player != null && Player.viewCamera != null ? Player.viewCamera : Camera.main;

            // The seed is hidden now: it only keeps a save's random rolls (blessings, plans,
            // altars) reproducible.
            Run = PendingLoad != null ? PendingLoad : new RunState { seed = Environment.TickCount };
            PendingLoad = null;
            Stats = new StatSheet();
            Run.energy = Balance.EnergyMax;

            Layout = SceneLayout.FromScene(gameObject.scene, Tree != null ? Tree.position : Vector3.zero);
            UnlockGate.ApplyAll(gameObject.scene, Run);
            GameEvents.RaiseStatsChanged();
            GameEvents.RaiseMapBuilt(Layout);
        }
    }
}
