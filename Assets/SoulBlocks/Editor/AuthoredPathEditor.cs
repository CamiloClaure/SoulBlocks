using UnityEditor;
using UnityEngine;

namespace SoulBlocks.Editor
{
    [CustomEditor(typeof(AuthoredPath))]
    public sealed class AuthoredPathEditor : UnityEditor.Editor
    {
        int selected;
        [MenuItem("Tools/SoulBlocks/Editar puntos del camino")]
        static void SelectPath()
        {
            var path = Object.FindAnyObjectByType<AuthoredPath>();
            if (path == null)
            {
                EditorUtility.DisplayDialog("Camino primario", "Abre Assets/Scenes/WorldBuilding.unity primero.", "Aceptar");
                return;
            }
            ShowPoints(path);
        }
        static void ShowPoints(AuthoredPath path)
        {
            Selection.activeGameObject = path.gameObject;
            var view = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView : EditorWindow.GetWindow<SceneView>();
            view.drawGizmos = true;
            var bounds = new Bounds(path.transform.position, Vector3.one * 4);
            foreach (var point in path.points) bounds.Encapsulate(path.transform.TransformPoint(point));
            bounds.Expand(4);
            view.Frame(bounds, false);
            view.Focus();
            SceneView.RepaintAll();
        }
        public override void OnInspectorGUI()
        {
            var path = (AuthoredPath)target;
            if (GUILayout.Button("Mostrar puntos en Scene")) ShowPoints(path);
            EditorGUILayout.HelpBox("Arrastra los puntos en Scene. Y controla la altura. Edita fuera de Play y guarda la escena (Ctrl+S). Evita curvas muy cerradas y pendientes verticales.", MessageType.Info);
            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            if (EditorGUI.EndChangeCheck()) { path.Rebuild(); SceneView.RepaintAll(); }
            selected = EditorGUILayout.IntSlider("Punto seleccionado", selected, 0, Mathf.Max(0, path.points.Count - 1));
            if (GUILayout.Button("Añadir punto al final"))
            {
                Undo.RecordObject(path, "Añadir punto");
                int n = path.points.Count;
                path.points.Add(n == 0 ? Vector3.zero : path.points[n - 1] + (n > 1 ? path.points[n - 1] - path.points[n - 2] : Vector3.forward * 8));
                selected = path.points.Count - 1; Changed(path);
            }
            using (new EditorGUI.DisabledScope(path.points.Count < 2 || selected >= path.points.Count - 1))
                if (GUILayout.Button("Insertar después del seleccionado"))
                {
                    Undo.RecordObject(path, "Insertar punto");
                    path.points.Insert(selected + 1, path.Evaluate((selected + .5f) / (path.points.Count - 1)));
                    selected++; Changed(path);
                }
            using (new EditorGUI.DisabledScope(path.points.Count <= 2))
                if (GUILayout.Button("Eliminar punto seleccionado"))
                {
                    Undo.RecordObject(path, "Eliminar punto"); path.points.RemoveAt(selected);
                    selected = Mathf.Min(selected, path.points.Count - 1); Changed(path);
                }
        }
        static void Changed(AuthoredPath path) { EditorUtility.SetDirty(path); path.Rebuild(); SceneView.RepaintAll(); }
        void OnSceneGUI()
        {
            var path = (AuthoredPath)target;
            var previousColor = Handles.color;
            var previousDepth = Handles.zTest;
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
            for (int i = 0; i < path.points.Count; i++)
            {
                Handles.color = Color.yellow;
                Vector3 world = path.transform.TransformPoint(path.points[i]);
                Handles.Label(world + Vector3.up * .5f, "P" + i);
                if (Handles.Button(world, Quaternion.identity, HandleUtility.GetHandleSize(world) * .16f, HandleUtility.GetHandleSize(world) * .19f, Handles.SphereHandleCap)) { selected = i; Repaint(); }
                if (i != selected) continue;
                EditorGUI.BeginChangeCheck();
                var position = Handles.PositionHandle(world, Quaternion.identity);
                if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(path, "Mover punto del camino"); path.points[i] = path.transform.InverseTransformPoint(position); Changed(path); }
            }
            Vector3 previous = path.transform.TransformPoint(path.Evaluate(0));
            Handles.color = Color.yellow;
            for (int i = 1; i <= 100; i++) { var next = path.transform.TransformPoint(path.Evaluate(i / 100f)); Handles.DrawLine(previous, next); previous = next; }
            Handles.color = previousColor;
            Handles.zTest = previousDepth;
        }
    }
}
