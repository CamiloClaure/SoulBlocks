using UnityEditor;
using UnityEngine;

namespace SoulBlocks.Editor
{
    public static class SelectWorldTerrain
    {
        [MenuItem("Tools/SoulBlocks/Seleccionar terreno editable")]
        public static void SelectTerrain()
        {
            var terrain = Object.FindAnyObjectByType<Terrain>();
            if (terrain == null) { Debug.Log("Abre la escena WorldBuilding para esculpir su terreno."); return; }
            Selection.activeGameObject = terrain.gameObject;
            EditorGUIUtility.PingObject(terrain.gameObject);
        }
    }
}
