using UnityEditor;
using UnityEngine;

namespace CWGame.Editor
{
    [CustomEditor(typeof(UINavigationRoot))]
    public class UINavigationRootEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var root = (UINavigationRoot)target;
            EditorGUILayout.Space();

            if (GUILayout.Button("Open Navigation Editor"))
            {
                UINavigationEditorWindow.Open(root);
            }

            if (GUILayout.Button("Scan UIBase Children"))
            {
                Undo.RecordObject(root, "Scan UI Navigation Pages");
                int added = root.ScanPagesInChildren();
                EditorUtility.SetDirty(root);
                Debug.Log($"[UINavigationRoot] Added {added} UI pages.", root);
            }

            var problems = UINavigationValidator.Validate(root);
            if (problems.Count == 0)
            {
                EditorGUILayout.HelpBox("Navigation tree is valid.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox($"Navigation tree has {problems.Count} problem(s). Open the editor for details.", MessageType.Warning);
            }
        }
    }
}
