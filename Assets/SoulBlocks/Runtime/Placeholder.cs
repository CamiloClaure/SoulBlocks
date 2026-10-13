using UnityEngine;

namespace SoulBlocks
{
    // Primitives + MaterialPropertyBlock colour (text goes through Hud): the only "art" allowed in
    // obra gruesa. One shared URP Lit material, coloured per instance so nothing new leaks
    // into the asset database.
    public static class Placeholder
    {
        static Material sharedMaterial;

        static Material SharedMaterial()
        {
            if (sharedMaterial == null)
            {
                sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "SoulBlocks Placeholder" };
                sharedMaterial.EnableKeyword("_EMISSION");
            }
            return sharedMaterial;
        }

        public static GameObject Make(PrimitiveType type, Color color, string name, Transform parent, Vector3 position, Vector3 scale, bool keepCollider = false, float emission = 0f)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            if (!keepCollider)
            {
                var collider = go.GetComponent<Collider>();
                if (collider != null) Object.Destroy(collider);
            }
            go.GetComponent<Renderer>().sharedMaterial = SharedMaterial();
            SetColor(go, color, emission);
            return go;
        }

        // A custom mesh (path ribbons, cave shells) with the shared material and, unless told
        // otherwise, a matching MeshCollider.
        public static GameObject MakeMesh(Mesh mesh, Color color, string name, Transform parent, bool collider = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = SharedMaterial();
            if (collider) go.AddComponent<MeshCollider>().sharedMesh = mesh;
            SetColor(go, color);
            return go;
        }

        public static void SetColor(GameObject go, Color color, float emission = 0f)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) return;
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", color);
            block.SetColor("_EmissionColor", color * emission);
            renderer.SetPropertyBlock(block);
        }
    }
}
