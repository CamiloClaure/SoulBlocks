using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SoulBlocks.Editor
{
    public static class CrystalMiningSetup
    {
        public const string PrefabPath = "Assets/SoulBlocks/Prefabs/Crystals/Crystal.prefab";

        [MenuItem("Tools/SoulBlocks/Create Crystal Mining Prototype")]
        public static void Build()
        {
            Directory.CreateDirectory("Assets/SoulBlocks/Prefabs/Crystals");
            Directory.CreateDirectory("Assets/SoulBlocks/Materials");
            AssetDatabase.Refresh();
            var yellow = Material("Crystal Yellow", new Color(1f, .8f, .05f));
            var wood = Material("Pickaxe Handle", new Color(.35f, .18f, .07f));
            var metal = Material("Pickaxe Head", new Color(.55f, .65f, .75f));

            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            {
                var crystal = new GameObject("Crystal");
                crystal.AddComponent<BoxCollider>();
                var logic = crystal.AddComponent<BreakableCrystal>();
                var model = new GameObject("Model").transform;
                model.SetParent(crystal.transform, false);
                logic.model = model;
                Cube("Placeholder - replace with Blender model", model, Vector3.zero, Vector3.one, yellow);
                logic.ConfigureAppearance();
                PrefabUtility.SaveAsPrefabAsset(crystal, PrefabPath);
                Object.DestroyImmediate(crystal);
            }

            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SoulBlocks.unity");
            var player = Object.FindAnyObjectByType<SoulPlayer>();
            var tree = GameObject.Find("Wisdom Tree");
            if (player == null || tree == null) throw new System.InvalidOperationException("Player or Wisdom Tree missing.");
            if (GameObject.Find("Crystal Yellow - Tree") == null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), scene);
                instance.name = "Crystal Yellow - Tree";
                var point = tree.transform.position + new Vector3(2.5f, 0f, 1f);
                Physics.SyncTransforms();
                if (Physics.Raycast(point + Vector3.up * 5, Vector3.down, out var ground, 10, 1 << 8)) point.y = ground.point.y;
                instance.transform.position = point;
            }

            var attack = player.GetComponent<CrystalMiningAttack>() ?? player.gameObject.AddComponent<CrystalMiningAttack>();
            if (attack.hitbox == null)
            {
                var volume = new GameObject("Mining Hitbox");
                volume.transform.SetParent(player.transform, false);
                attack.hitbox = volume.AddComponent<BoxCollider>();
                attack.hitbox.center = new Vector3(0, .55f, .9f);
                attack.hitbox.size = new Vector3(.85f, 1.1f, 1.3f);
                attack.hitbox.isTrigger = true;
                attack.hitbox.enabled = false;
            }
            if (attack.pickaxe == null)
            {
                var hand = player.animator != null && player.animator.isHuman
                    ? player.animator.GetBoneTransform(HumanBodyBones.RightHand) : player.visual;
                if (hand == null) hand = player.transform;
                var pick = new GameObject("Mining Pickaxe");
                pick.transform.SetParent(hand, false);
                var scale = hand.lossyScale;
                pick.transform.localScale = new Vector3(1 / scale.x, 1 / scale.y, 1 / scale.z);
                Cube("Handle", pick.transform, new Vector3(0, .15f, 0), new Vector3(.045f, .65f, .045f), wood);
                Cube("Head", pick.transform, new Vector3(0, .46f, 0), new Vector3(.42f, .085f, .09f), metal);
                attack.pickaxe = pick;
                pick.SetActive(false);
            }
            EditorUtility.SetDirty(attack);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Crystal mining ready: authored yellow prefab beside tree, three hits, mining on Player.");
        }

        static Material Material(string name, Color color)
        {
            string path = "Assets/SoulBlocks/Materials/" + name + ".mat";
            var result = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (result != null) return result;
            result = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            result.SetColor("_BaseColor", color);
            result.SetFloat("_Smoothness", .35f);
            AssetDatabase.CreateAsset(result, path);
            return result;
        }

        static void Cube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = position;
            cube.transform.localScale = scale;
            Object.DestroyImmediate(cube.GetComponent<Collider>());
            cube.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}
