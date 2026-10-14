using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SoulBlocks.Editor
{
    public static class SpecialBlockLabSetup
    {
        const string Folder = "Assets/SoulBlocks/Prefabs/SpecialBlocks";
        [MenuItem("Tools/SoulBlocks/Create Special Block Lab")]
        public static void Build()
        {
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SoulBlocks.unity");
            var tree = GameObject.Find("Wisdom Tree");
            if (tree == null) throw new System.InvalidOperationException("Wisdom Tree is missing.");
            var oldLab = Object.FindAnyObjectByType<SpecialBlockLab>();
            if (oldLab != null) Object.DestroyImmediate(oldLab.gameObject);
            var oldCrystal = GameObject.Find("Crystal Yellow - Tree");
            if (oldCrystal != null) Object.DestroyImmediate(oldCrystal);
            var labRoot = new GameObject("Special Block Lab");
            labRoot.transform.position = tree.transform.position;
            var lab = labRoot.AddComponent<SpecialBlockLab>();
            lab.tree = tree.transform;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CrystalMiningSetup.PrefabPath);
            if (prefab == null) throw new System.InvalidOperationException("Crystal prefab is missing.");
            var dark = Material("Dark Path", new Color(.018f, .02f, .025f));
            var slots = new List<CrystalLabSlot>();
            var surfaces = new List<LabSurface>();
            var displays = new List<Renderer>();
            var colors = new[] { CrystalColor.Yellow, CrystalColor.Blue, CrystalColor.Green, CrystalColor.Celeste, CrystalColor.Red };
            Physics.SyncTransforms();
            for (int i = 0; i < 5; i++)
            {
                var block = (SpecialBlock)i;
                var material = Material(BlockRules.Name(block), BlockRules.ColorOf(block));
                string path = Folder + "/" + BlockRules.Name(block) + ".prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                {
                    var display = new GameObject("Bloque " + BlockRules.Name(block));
                    Cube("Model", display.transform, Vector3.up * .7f, Vector3.one * .55f, material, false);
                    var pedestal = display.AddComponent<SpecialBlockPedestal>();
                    pedestal.block = block;
                    pedestal.label = Label(display.transform, BlockRules.Name(block) + "\nNv 1", Vector3.up * 1.4f, Color.white, .18f);
                    PrefabUtility.SaveAsPrefabAsset(display, path);
                    Object.DestroyImmediate(display);
                }
                var blockObject = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), labRoot.transform);
                blockObject.transform.position = Ground(tree.transform.position + new Vector3((i - 2) * 1.2f, 0, 2.2f));
                displays.Add(blockObject.GetComponentInChildren<Renderer>());
                float angle = (-80 + i * 40) * Mathf.Deg2Rad;
                Vector3 direction = new Vector3(Mathf.Sin(angle), 0, -Mathf.Cos(angle));
                for (int size = 1; size <= 3; size++)
                {
                    var marker = new GameObject(BlockRules.Name(block) + " · " + (CrystalSize)size);
                    marker.transform.SetParent(labRoot.transform);
                    marker.transform.position = Ground(tree.transform.position + direction * (3.3f + (size - 1) * 2.4f));
                    var slot = marker.AddComponent<CrystalLabSlot>();
                    slot.prefab = prefab; slot.color = colors[i]; slot.size = (CrystalSize)size; slot.hidden = i == 1;
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, marker.transform);
                    go.transform.localPosition = Vector3.zero;
                    go.name = i == 0 && size == 1 ? "Crystal Yellow - Tree" : "Crystal " + colors[i] + " " + (CrystalSize)size;
                    var crystal = go.GetComponent<BreakableCrystal>();
                    crystal.color = colors[i]; crystal.size = (CrystalSize)size; crystal.startsHidden = slot.hidden; crystal.rewardSouls = 3 * size;
                    // A distinct material also makes each color visible before Play (property blocks are not serialized).
                    crystal.model.GetComponentInChildren<Renderer>().sharedMaterial = material;
                    crystal.ConfigureAppearance();
                    PrefabUtility.RecordPrefabInstancePropertyModifications(crystal);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(crystal.GetComponent<BoxCollider>());
                    PrefabUtility.RecordPrefabInstancePropertyModifications(crystal.model);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(crystal.model.GetComponentInChildren<Renderer>());
                    slot.crystal = crystal;
                    slots.Add(slot);
                }
                for (int segment = 0; segment < 6; segment++)
                {
                    var position = Ground(tree.transform.position + direction * (2.6f + segment * 1.15f));
                    var tile = Cube("Camino oscuro " + i + "-" + segment, labRoot.transform, Vector3.zero, new Vector3(1.6f, .04f, 1.25f), dark, true);
                    tile.transform.position = position + Vector3.up * .015f;
                    tile.transform.rotation = Quaternion.LookRotation(direction);
                    tile.layer = 8;
                    var surface = tile.AddComponent<LabSurface>(); surface.baseColor = dark.color; surface.darkPath = true;
                    surfaces.Add(surface);
                }
                Label(labRoot.transform, BlockRules.Name(block).ToUpperInvariant() + (i == 1 ? "\nOCULTOS: usa azul" : ""),
                    direction * 9.6f + Vector3.up * .2f, BlockRules.ColorOf(block), .2f).transform.rotation = Quaternion.Euler(60, 0, 0);
            }
            lab.slots = slots.ToArray(); lab.surfaces = surfaces.ToArray(); lab.blockDisplays = displays.ToArray();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("SPECIAL_BLOCK_LAB_READY: 15 authored crystals, 5 tree blocks, 30 dark path segments.");
        }
        static Vector3 Ground(Vector3 point)
        {
            if (Physics.Raycast(point + Vector3.up * 4, Vector3.down, out var hit, 8, 1 << 8)) point.y = hit.point.y + .015f;
            return point;
        }
        static Material Material(string name, Color color)
        {
            string path = Folder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = color;
            material.EnableKeyword("_EMISSION");
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
        static GameObject Cube(string name, Transform parent, Vector3 local, Vector3 scale, Material material, bool collider)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name; go.transform.SetParent(parent, false); go.transform.localPosition = local; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }
        static TextMesh Label(Transform parent, string text, Vector3 local, Color color, float size)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false); go.transform.localPosition = local;
            var label = go.AddComponent<TextMesh>(); label.text = text; label.characterSize = size; label.anchor = TextAnchor.MiddleCenter; label.color = color;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            go.GetComponent<Renderer>().sharedMaterial = label.font.material;
            return label;
        }
    }
}
