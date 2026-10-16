using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SoulBlocks.Editor
{
    public static class WorldBuildingSetup
    {
        public const string ScenePath = "Assets/Scenes/WorldBuilding.unity";
        [MenuItem("Tools/SoulBlocks/Open World Building")]
        public static void Build()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (System.IO.File.Exists(ScenePath)) { EditorSceneManager.OpenScene(ScenePath); return; }
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SoulBlocks.unity");
            var lab = Object.FindAnyObjectByType<SpecialBlockLab>();
            if (lab != null) Object.DestroyImmediate(lab.gameObject);
            var world = new GameObject("Mundo manual"); world.AddComponent<ManualWorld>();
            var tree = GameObject.Find("Wisdom Tree").transform;
            var island = GameObject.Find("Main Island").transform;
            Physics.SyncTransforms();
            var floors = island.GetComponentsInChildren<Collider>().Where(c => c.enabled && !c.isTrigger).ToArray();
            float edge = floors.Max(c => c.bounds.max.z);
            float height = floors.Max(c => c.bounds.max.y) + .03f;
            var go = new GameObject("Camino primario"); go.transform.SetParent(world.transform);
            go.transform.position = new Vector3(tree.position.x, height, tree.position.z + 3);
            var path = go.AddComponent<AuthoredPath>();
            float distance = Mathf.Max(8, edge - go.transform.position.z);
            path.points = new List<Vector3> { Vector3.zero, new Vector3(0, 0, distance * .5f), new Vector3(0, 0, distance), new Vector3(5, 1, distance + 10), new Vector3(-4, 2, distance + 21), new Vector3(0, 3, distance + 32) };
            path.width = 4; path.Rebuild();
            string materialPath = "Assets/SoulBlocks/Materials/ManualPath.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); material.color = new Color(.35f, .30f, .23f); AssetDatabase.CreateAsset(material, materialPath); }
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            var end = GameObject.CreatePrimitive(PrimitiveType.Cube);
            end.name = "Destino provisional"; end.transform.SetParent(world.transform);
            end.transform.position = go.transform.TransformPoint(path.points[path.points.Count - 1]) + Vector3.forward * 2 + Vector3.down * .25f;
            end.transform.localScale = new Vector3(8, .5f, 8); end.layer = 8;
            end.GetComponent<MeshRenderer>().sharedMaterial = material;
            var player = Object.FindAnyObjectByType<SoulPlayer>();
            player.transform.position = go.transform.position + Vector3.up * .1f;
            EditorSceneManager.SaveScene(scene, ScenePath);
            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(s => s.path == ScenePath)) { scenes.Add(new EditorBuildSettingsScene(ScenePath, true)); EditorBuildSettings.scenes = scenes.ToArray(); }
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = go;
            Debug.Log("WORLD_BUILDING_READY: authored path and manual scene saved.");
        }
    }
}
