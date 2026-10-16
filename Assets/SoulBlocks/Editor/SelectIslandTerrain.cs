using UnityEditor;
using UnityEngine;

namespace SoulBlocks.Editor
{
    // Menu shortcut that selects the island terrain so its sculpt and paint tools are one click away.
    public static class SelectIslandTerrain
    {
        [MenuItem("Tools/SoulBlocks/Seleccionar terreno de la isla")]
        public static void SelectTerrain()
        {
            var terrain = Object.FindAnyObjectByType<Terrain>();
            if (terrain == null)
            { Debug.Log("Abre la escena SoulBlocks para esculpir su terreno."); return; }
            Selection.activeGameObject = terrain.gameObject;
            EditorGUIUtility.PingObject(terrain.gameObject);
        }
    }
}
