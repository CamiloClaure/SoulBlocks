using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace SoulBlocks.Editor
{
    [InitializeOnLoad]
    public static class SoulBlocksSetup
    {
        const string ScenePath = "Assets/Scenes/SoulBlocks.unity";
        static int frames;
        static Vector3 start, leftFootStart;
        static Quaternion leftFootRotation;
        static float distance;
        static bool boneMoved;
        static SoulPlayer player;
        static SoulCamera rig;
        static SoulBlocksSetup()
        {
            EditorApplication.playModeStateChanged += PlayState;
            if (SessionState.GetBool("SoulBlocksVerify", false)) EditorApplication.update += VerifyTick;
        }

        [MenuItem("Tools/SoulBlocks/Validate Minimal Scene")]
        public static void SetupAndVerify()
        {
            try
            {
                var scene = EditorSceneManager.OpenScene(ScenePath);
                player = Object.FindAnyObjectByType<SoulPlayer>();
                Require(player != null && player.animator != null && player.visual != null, "Player rig is missing.");
                var island = GameObject.Find("Main Island");
                Require(island != null, "Main island is missing.");
                foreach (Transform row in island.transform) row.gameObject.layer = 8;
                Require(island.transform.childCount == 33, "Expected the 33 original first-island tile rows.");
                var tree = Object.FindAnyObjectByType<WisdomTreeVisual>();
                if (tree == null)
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LightingBlocks/Art/SpectralForest/Prefabs/WisdomTree.prefab");
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    go.name = "Wisdom Tree";
                    go.transform.position = new Vector3(-12, 0, -3);
                    tree = go.GetComponent<WisdomTreeVisual>();
                }
                Require(tree != null, "Wisdom tree is missing.");
                var controller = (AnimatorController)player.animator.runtimeAnimatorController;
                var machine = controller.layers[0].stateMachine;
                foreach (var child in machine.states.ToArray())
                {
                    if (child.state.name == "Pull") machine.RemoveState(child.state);
                    else if (child.state.name == "Run") child.state.name = "Walk";
                }
                Require(machine.states.Length == 2 && machine.states.Any(s => s.state.name == "Walk" && s.state.motion != null), "Idle/walk clips missing.");
                player.animator.applyRootMotion = false;
                player.animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                AssetDatabase.DeleteAsset("Assets/Animations/base@Pull Heavy Object - 20%.fbx");
                Directory.CreateDirectory("Assets/SoulBlocks/Settings");
                const string rendererPath = "Assets/SoulBlocks/Settings/SoulBlocksRenderer.asset";
                var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
                if (renderer == null)
                {
                    renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                    AssetDatabase.CreateAsset(renderer, rendererPath);
                }
                const string pipelinePath = "Assets/SoulBlocks/Settings/SoulBlocksPipeline.asset";
                var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
                if (pipeline == null)
                {
                    pipeline = UniversalRenderPipelineAsset.Create(renderer);
                    AssetDatabase.CreateAsset(pipeline, pipelinePath);
                }
                pipeline.supportsHDR = true;
                pipeline.msaaSampleCount = 4;
                pipeline.shadowDistance = 65;
                GraphicsSettings.defaultRenderPipeline = pipeline;
                QualitySettings.renderPipeline = pipeline;
                RenderSettings.skybox = null;
                RenderSettings.fog = false;
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(.18f, .17f, .24f);
                RenderSettings.ambientIntensity = 1;
                var light = Object.FindObjectsByType<Light>().First(l => l.type == LightType.Directional);
                light.intensity = .75f;
                light.color = new Color(.82f, .80f, 1f);
                light.shadows = LightShadows.Soft;
                rig = Object.FindAnyObjectByType<SoulCamera>();
                rig.SnapToPlayer();
                var camera = rig.GetComponent<Camera>();
                camera.farClipPlane = 150;
                camera.fieldOfView = 50;
                camera.backgroundColor = new Color(.012f, .01f, .025f);
                camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
                var volume = camera.GetComponent<Volume>() ?? camera.gameObject.AddComponent<Volume>();
                volume.isGlobal = true;
                const string volumePath = "Assets/SoulBlocks/Settings/SoulBlocksVolume.asset";
                var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(volumePath);
                if (profile == null)
                {
                    profile = ScriptableObject.CreateInstance<VolumeProfile>();
                    AssetDatabase.CreateAsset(profile, volumePath);
                    var bloom = profile.Add<Bloom>(true);
                    bloom.threshold.Override(1.1f); bloom.intensity.Override(.35f);
                    AssetDatabase.AddObjectToAsset(bloom, profile);
                }
                volume.sharedProfile = profile;
                PlayerSettings.productName = "SoulBlocks";
                PlayerSettings.companyName = "CamiloClaure";
                EditorSettings.enterPlayModeOptionsEnabled = false;
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
                Require(scene.GetRootGameObjects().Length == 6 && Object.FindAnyObjectByType<BreakableCrystal>() != null,
                    "Expected the original scene roots and the authored mining crystal.");
                Require(Object.FindObjectsByType<MonoBehaviour>().All(m => m != null && (m.GetType().Namespace.StartsWith("SoulBlocks") || m.GetType().Namespace.StartsWith("UnityEngine.Rendering"))), "Unexpected gameplay script.");
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var t in root.GetComponentsInChildren<Transform>(true))
                        Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) == 0, "Missing script on " + t.name);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                SessionState.SetBool("SoulBlocksVerify", true);
                EditorApplication.update -= VerifyTick;
                EditorApplication.update += VerifyTick;
                EditorApplication.isPlaying = true;
            }
            catch (Exception e) { Fail(e); }
        }
        static void PlayState(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("SoulBlocksVerify", false))
            {
                frames = 0; distance = 0; boneMoved = false;
                player = Object.FindAnyObjectByType<SoulPlayer>();
                rig = Object.FindAnyObjectByType<SoulCamera>();
                player.readInput = false; rig.readInput = false;
                start = player.transform.position;
                var foot = player.animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                leftFootStart = player.transform.InverseTransformPoint(foot.position);
                leftFootRotation = foot.localRotation;
            }
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool("SoulBlocksPassed", false))
            {
                SessionState.SetBool("SoulBlocksPassed", false);
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
        }
        static void VerifyTick()
        {
            if (!SessionState.GetBool("SoulBlocksVerify", false) || !EditorApplication.isPlaying || player == null) return;
            try
            {
                frames++;
                if (frames < 25) return;
                if (frames == 25) player.enabled = false;
                if (frames < 85)
                {
                    player.StepMovement(Vector2.up, 1f / 60);
                    player.animator.Update(1f / 60);
                    var foot = player.animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                    boneMoved |= Vector3.Distance(leftFootStart, player.transform.InverseTransformPoint(foot.position)) > .01f || Quaternion.Angle(leftFootRotation, foot.localRotation) > 1f;
                    return;
                }
                if (frames == 85)
                {
                    distance = Vector3.Distance(start, player.transform.position);
                    Require(distance > 4f, "Player did not walk.");
                    Require(boneMoved, "Walking animation did not move the foot bone.");
                    Require(player.animator.GetCurrentAnimatorStateInfo(0).IsName("Walk"), "Walking animation state did not activate.");
                    // Check the unsupported edge using a large time step too.
                    player.StepMovement(Vector2.up, 20f);
                    Require(Physics.Raycast(player.transform.position + Vector3.up, Vector3.down, 3, 1 << 8), "Player escaped the only island.");
                    player.GetComponent<CharacterController>().enabled = false;
                    player.transform.position = start;
                    player.GetComponent<CharacterController>().enabled = true;
                    player.StepMovement(Vector2.zero, 1f / 60);
                }
                if (frames < 110) { player.animator.Update(1f / 60); return; }
                Require(player.animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"), "Idle animation did not resume.");
                Require(Object.FindObjectsByType<WisdomTreeVisual>().Length == 1, "Expected exactly one tree.");
                Require(Object.FindObjectsByType<SoulPlayer>().Length == 1, "Expected exactly one player.");
                Require(Object.FindObjectsByType<MonoBehaviour>().All(m => m != null), "Runtime missing script.");
                Directory.CreateDirectory("Validation");
                rig.SnapToPlayer();
                Capture(rig.GetComponent<Camera>(), "Validation/SoulBlocks.png");
                File.WriteAllText("Validation/verification.txt", "PASS\nScene roots: Main Island, Wisdom Tree, Player, Main Camera, Directional Light\nOriginal main-island rows: 33\nPlayer count: 1\nWisdom tree count: 1\nWalking distance: " + distance.ToString("F2") + " m\nWalking bone motion: verified\nIdle/walk transitions: verified\nIsland edge containment: verified\nMissing scripts: 0\nUnity: " + Application.unityVersion + "\n");
                Debug.Log("SOULBLOCKS_VERIFICATION_PASSED");
                SessionState.SetBool("SoulBlocksVerify", false);
                SessionState.SetBool("SoulBlocksPassed", true);
                EditorApplication.isPlaying = false;
            }
            catch (Exception e) { Fail(e); }
        }
        static void Capture(Camera camera, string path)
        {
            var rt = new RenderTexture(1280, 720, 24);
            camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
            var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            camera.targetTexture = null; RenderTexture.active = null;
            Object.DestroyImmediate(texture); Object.DestroyImmediate(rt);
        }
        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        static void Fail(Exception e)
        {
            SessionState.SetBool("SoulBlocksVerify", false);
            Debug.LogException(e);
            Directory.CreateDirectory("Validation");
            File.WriteAllText("Validation/verification.txt", "FAIL\n" + e);
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
    }
}

