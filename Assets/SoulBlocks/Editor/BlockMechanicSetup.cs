using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class BlockMechanicSetup
{
    const string RequestPath = "Assets/SoulBlocks/Editor/InstallBlockMechanic.request";
    const string ScenePath = "Assets/Scenes/SoulBlocks.unity";
    const string MaterialFolder = "Assets/SoulBlocks/Materials";
    const string TestKey = "SoulBlocks.BlockMechanicValidation";
    static int validationFrames;

    static BlockMechanicSetup()
    {
        EditorApplication.update += CheckInstallRequest;
        EditorApplication.playModeStateChanged += PlayStateChanged;
        if (SessionState.GetBool(TestKey, false)) EditorApplication.update += ValidatePlay;
    }

    static void CheckInstallRequest()
    {
        if (Application.isBatchMode || !File.Exists(RequestPath) || EditorApplication.isCompiling ||
            EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene active = SceneManager.GetActiveScene();
        if (!active.IsValid() || !active.isLoaded || FindInScene<SoulBlocks.SoulPlayer>(active) == null) return;
        try
        {
            string backup = "Assets/Scenes/SoulBlocks_BeforeBlockMechanic.unity";
            if (!File.Exists(backup)) EditorSceneManager.SaveScene(active, backup, true);
            Configure(active);
            EditorSceneManager.SaveScene(active);
            AssetDatabase.DeleteAsset(RequestPath);
            Directory.CreateDirectory("Validation");
            File.WriteAllText("Validation/block-mechanic-install.txt", "Installed into " + active.path + "\n");
            Debug.Log("SoulBlocks: block mechanic installed. Hold E to drag, release E to place.");
        }
        catch (Exception exception)
        {
            EditorApplication.update -= CheckInstallRequest;
            Debug.LogException(exception);
        }
    }

    [MenuItem("Tools/SoulBlocks/Configure Block Mechanic")]
    public static void ConfigureActiveScene()
    {
        Configure(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
    }

    public static void SetupBatch()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath);
        Configure(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    static T FindInScene<T>(Scene scene) where T : Component => scene.GetRootGameObjects()
        .SelectMany(root => root.GetComponentsInChildren<T>(true)).FirstOrDefault();

    static Material MakeMaterial(string name, Color color, float emission)
    {
        if (!AssetDatabase.IsValidFolder(MaterialFolder))
            AssetDatabase.CreateFolder("Assets/SoulBlocks", "Materials");
        string path = MaterialFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP Lit shader is missing.");
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Smoothness", .25f);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", color * emission);
        EditorUtility.SetDirty(material);
        return material;
    }

    static void Configure(Scene scene)
    {
        SoulBlocks.SoulPlayer player = FindInScene<SoulBlocks.SoulPlayer>(scene);
        GameObject island = scene.GetRootGameObjects().FirstOrDefault(root => root.name == "Main Island");
        if (player == null || island == null) throw new InvalidOperationException("Open the SoulBlocks scene first.");
        foreach (Collider ground in island.GetComponentsInChildren<Collider>()) ground.gameObject.layer = 8;
        UnityEngine.Physics.IgnoreLayerCollision(0, 9, false);
        SerializedObject tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        tags.FindProperty("layers").GetArrayElementAtIndex(9).stringValue = "PlacedBlock";
        tags.ApplyModifiedPropertiesWithoutUndo();

        Material yellow = MakeMaterial("Block Yellow", PickableBlock.ColorFor(BlockColor.Yellow), .3f);
        Material dark = MakeMaterial("Closed Path", new Color(.025f, .021f, .035f), .04f);
        Material marker = MakeMaterial("Yellow Path Marker", new Color(.55f, .35f, .025f), .4f);
        const string prefabPath = "Assets/Prefabs/YellowCube.prefab";
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            prefabRoot.transform.localPosition = Vector3.zero;
            prefabRoot.transform.localRotation = Quaternion.identity;
            prefabRoot.transform.localScale = Vector3.one * .8f;
            prefabRoot.layer = 0;
            PickableBlock block = prefabRoot.GetComponent<PickableBlock>() ?? prefabRoot.AddComponent<PickableBlock>();
            block.blockColor = BlockColor.Yellow;
            BoxCollider collider = prefabRoot.GetComponent<BoxCollider>();
            collider.center = Vector3.zero;
            collider.size = Vector3.one;
            collider.isTrigger = false;
            collider.enabled = true;
            prefabRoot.GetComponent<MeshRenderer>().sharedMaterial = yellow;
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(prefabRoot); }

        BlockInteraction interaction = player.GetComponent<BlockInteraction>() ?? player.gameObject.AddComponent<BlockInteraction>();
        interaction.viewCamera = player.viewCamera;
        interaction.grabRange = 2.5f;
        interaction.holdDistance = 1.2f;
        interaction.holdHeight = .55f;
        interaction.placementRange = 1.8f;
        interaction.readInput = true;

        BlockGenerator generator = FindInScene<BlockGenerator>(scene);
        if (generator == null)
        {
            GameObject root = new GameObject("Block Generator");
            SceneManager.MoveGameObjectToScene(root, scene);
            generator = root.AddComponent<BlockGenerator>();
        }
        generator.blockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        generator.mainIsland = island.transform;
        generator.player = player.transform;
        generator.useIslandBounds = true;
        generator.spawnStarterBlocks = true;
        generator.blockCount = 30;
        generator.groundMask = 1 << 8;
        generator.minimumSpacing = 2f;
        generator.surfaceOffset = .42f;
        generator.maxSpawnAttempts = 10000;
        foreach (BlockGenerator duplicate in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<BlockGenerator>()))
            if (duplicate != generator) duplicate.enabled = false;

        GameObject paths = scene.GetRootGameObjects().FirstOrDefault(root => root.name == "Unlockable Paths");
        if (paths == null)
        {
            paths = new GameObject("Unlockable Paths");
            SceneManager.MoveGameObjectToScene(paths, scene);
        }
        if (paths.GetComponentsInChildren<PathSlot>().Length == 0)
        {
            Physics.SyncTransforms();
            Collider[] ground = island.GetComponentsInChildren<Collider>().Where(c => c.enabled && !c.isTrigger).ToArray();
            BuildRoad(paths.transform, ground, "North Yellow Path", Vector3.forward, -5f, dark, marker);
            BuildRoad(paths.transform, ground, "East Yellow Path", Vector3.right, -12f, dark, marker);
            BuildRoad(paths.transform, ground, "West Yellow Path", Vector3.left, -30f, dark, marker);
        }
        EditorUtility.SetDirty(generator);
        EditorUtility.SetDirty(interaction);
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
    }

    static void BuildRoad(Transform parent, Collider[] ground, string name, Vector3 direction,
        float crossCoordinate, Material dark, Material marker)
    {
        bool alongX = Mathf.Abs(direction.x) > .5f;
        bool positive = direction.x + direction.z > 0;
        Collider[] matching = ground.Where(c => crossCoordinate >= (alongX ? c.bounds.min.z : c.bounds.min.x) + .01f &&
            crossCoordinate <= (alongX ? c.bounds.max.z : c.bounds.max.x) - .01f).ToArray();
        if (matching.Length == 0) throw new InvalidOperationException("No island edge found for " + name);
        float boundary = positive ? matching.Max(c => alongX ? c.bounds.max.x : c.bounds.max.z)
            : matching.Min(c => alongX ? c.bounds.min.x : c.bounds.min.z);
        float top = matching.Max(c => c.bounds.max.y);
        Vector3 edge = alongX ? new Vector3(boundary, top, crossCoordinate) : new Vector3(crossCoordinate, top, boundary);
        Transform road = new GameObject(name).transform;
        road.SetParent(parent, false);
        PathSlot previous = null;
        for (int i = 0; i < 6; i++)
        {
            GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tile.name = "Yellow Slot " + (i + 1).ToString("00");
            tile.transform.SetParent(road, false);
            tile.transform.position = edge + direction * (1f + i * 2f) - Vector3.up * .125f;
            tile.transform.localScale = new Vector3(2, .25f, 2);
            tile.GetComponent<MeshRenderer>().sharedMaterial = dark;
            tile.GetComponent<BoxCollider>().isTrigger = true;
            PathSlot slot = tile.AddComponent<PathSlot>();
            slot.previousSlot = previous;
            previous = slot;
            GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rim.name = "Yellow color marker";
            Object.DestroyImmediate(rim.GetComponent<BoxCollider>());
            rim.transform.SetParent(tile.transform, false);
            rim.transform.localPosition = new Vector3(0, .53f, 0);
            rim.transform.localScale = alongX ? new Vector3(.045f, .04f, .8f) : new Vector3(.8f, .04f, .045f);
            rim.GetComponent<MeshRenderer>().sharedMaterial = marker;
        }
    }

    [MenuItem("Tools/SoulBlocks/Validate Block Mechanic")]
    public static void ValidateBatch()
    {
        try
        {
            SetupBatch();
            SessionState.SetBool(TestKey, true);
            validationFrames = 0;
            EditorApplication.isPlaying = true;
        }
        catch (Exception exception) { Fail(exception); }
    }

    static void PlayStateChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(TestKey, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        { validationFrames = 0; EditorApplication.update -= ValidatePlay; EditorApplication.update += ValidatePlay; }
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(TestKey, false);
            if (Application.isBatchMode) EditorApplication.Exit(SessionState.GetBool(TestKey + ".Passed", false) ? 0 : 1);
        }
    }

    static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }

    static void Teleport(SoulBlocks.SoulPlayer player, Vector3 position)
    {
        CharacterController controller = player.GetComponent<CharacterController>();
        controller.enabled = false;
        player.transform.position = position;
        controller.enabled = true;
        Physics.SyncTransforms();
    }

    static void ValidatePlay()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling || ++validationFrames < 30) return;
        EditorApplication.update -= ValidatePlay;
        try
        {
            SoulBlocks.SoulPlayer player = Object.FindAnyObjectByType<SoulBlocks.SoulPlayer>();
            player.readInput = false;
            player.enabled = false;
            BlockInteraction interaction = player.GetComponent<BlockInteraction>();
            interaction.readInput = false;
            BlockGenerator generator = Object.FindAnyObjectByType<BlockGenerator>();
            PickableBlock[] blocks = Object.FindObjectsByType<PickableBlock>();
            PathSlot[] slots = Object.FindObjectsByType<PathSlot>();
            Require(generator.SpawnedCount == 30 && blocks.Length == 30, "Expected 30 generated blocks.");
            Require(slots.Length == 18, "Expected three paths of six tiles.");
            for (int i = 0; i < blocks.Length; i++)
            {
                Require(Physics.Raycast(blocks[i].transform.position, Vector3.down, 2f, 1 << 8), "Block spawned off island.");
                for (int j = i + 1; j < blocks.Length; j++)
                    Require(Vector3.Distance(blocks[i].transform.position, blocks[j].transform.position) >= 1.99f,
                        "Generated blocks overlap.");
            }
            foreach (PathSlot slot in slots)
                Require(!Physics.Raycast(slot.transform.position + Vector3.up * 2, Vector3.down, 3f,
                    (1 << 8) | (1 << 9), QueryTriggerInteraction.Ignore), "Closed path already provides support.");

            PathSlot first = slots.First(slot => slot.previousSlot == null && slot.transform.parent.name == "North Yellow Path");
            PathSlot second = slots.First(slot => slot.previousSlot == first);
            GameObject testObject = Object.Instantiate(generator.blockPrefab, new Vector3(0, 10, 0), Quaternion.identity);
            PickableBlock testBlock = testObject.GetComponent<PickableBlock>();
            testBlock.blockColor = BlockColor.Blue;
            Require(!first.TryPlace(testBlock), "Yellow path accepted a blue block.");
            testBlock.blockColor = BlockColor.Yellow;
            Require(!second.TryPlace(testBlock), "Path accepted a block out of order.");
            Object.DestroyImmediate(testObject);

            player.viewCamera = null;
            Vector3 approach = first.transform.position - Vector3.forward * 1.2f;
            approach.y = .025f;
            Teleport(player, approach);
            player.StepMovement(Vector2.up, 1f);
            Require(player.transform.position.z < first.transform.position.z - .8f,
                "Player walked onto a closed path.");

            PickableBlock nearest = blocks.OrderBy(block => Vector3.Distance(block.transform.position, player.transform.position)).First();
            Teleport(player, nearest.transform.position - Vector3.forward * 1.2f + Vector3.down * .395f);
            Require(interaction.BeginHold() && interaction.HeldBlock == nearest,
                "Nearest-block pickup failed.");
            Require(!nearest.SolidCollider.enabled, "Held block still collides with player.");
            if (player.visual != null) player.visual.rotation = Quaternion.identity;
            Teleport(player, approach);
            interaction.UpdateHeldPosition();
            Require(interaction.HeldBlock == nearest, "Held block switched while moving.");
            Require(interaction.ReleaseHeldBlock(), "Releasing near first slot did not place block.");
            Require(first.IsUnlocked && nearest.IsPlaced && nearest.gameObject.layer == 9 && nearest.SolidCollider.enabled,
                "Placed block was not made solid and walkable.");
            Require(Physics.Raycast(first.transform.position + Vector3.up * 2, Vector3.down, 3f, 1 << 9),
                "Placed block does not provide support.");
            player.StepMovement(Vector2.up, .5f);
            Require(player.transform.position.z > first.transform.position.z + .3f,
                "Player did not walk over the placed block.");
            Require(player.transform.position.z < second.transform.position.z - .8f,
                "Player crossed the next closed tile.");

            Teleport(player, approach);
            PickableBlock available = blocks.First(block => !block.IsPlaced);
            available.transform.position = approach + Vector3.left * 1.1f + Vector3.up * .395f;
            Physics.SyncTransforms();
            Require(interaction.BeginHold() && interaction.HeldBlock == available, "Second pickup failed.");
            Vector3 dropApproach = new Vector3(-5, .025f, 3);
            Teleport(player, dropApproach);
            if (player.visual != null) player.visual.rotation = Quaternion.Euler(0, 180, 0);
            interaction.UpdateHeldPosition();
            interaction.ReleaseHeldBlock();
            Require(!available.IsHeld && available.SolidCollider.enabled &&
                Physics.Raycast(available.transform.position, Vector3.down, 2f, 1 << 8), "Dropping on island failed.");
            Require(interaction.BeginHold(), "Pickup after drop failed.");
            interaction.CancelHold();
            Require(interaction.HeldBlock == null && available.SolidCollider.enabled, "Cancel did not restore collider.");
            Require(Object.FindObjectsByType<MonoBehaviour>().All(script => script != null),
                "Missing runtime scripts.");

            var camera = Object.FindAnyObjectByType<SoulBlocks.SoulCamera>();
            camera.readInput = false;
            Teleport(player, new Vector3(-5, .025f, 5));
            if (player.visual != null) player.visual.rotation = Quaternion.identity;
            camera.SnapToPlayer();
            Directory.CreateDirectory("Validation");
            File.WriteAllText("Validation/block-mechanic-tests.txt",
                "PASS\n30 blocks distributed without overlap\n18 dark tiles without walkable support\nWrong color rejected\nOut-of-order placement rejected\nNearest block selected automatically\nHeld block remains selected while moving\nRelease snaps block and enables solid support\nPlayer walks over filled tile and stops before dark tile\nSafe ground drop and cancellation\nNo missing scripts\nUnity " + Application.unityVersion + "\n");
            SessionState.SetBool(TestKey + ".Passed", true);
            Debug.Log("BLOCK_MECHANIC_VALIDATION_PASS");
            EditorApplication.isPlaying = false;
        }
        catch (Exception exception) { Fail(exception); }
    }

    static void Fail(Exception exception)
    {
        EditorApplication.update -= ValidatePlay;
        SessionState.SetBool(TestKey + ".Passed", false);
        Directory.CreateDirectory("Validation");
        File.WriteAllText("Validation/block-mechanic-tests.txt", "FAIL\n" + exception);
        Debug.LogException(exception);
        if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
        else if (Application.isBatchMode) EditorApplication.Exit(1);
    }
}

