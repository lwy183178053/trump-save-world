using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CWGame.Editor
{
    [CustomEditor(typeof(UINavButton))]
    public class UINavButtonEditor : UnityEditor.Editor
    {
        private SerializedProperty navigatorProperty;
        private SerializedProperty targetNodeIdProperty;
        private SerializedProperty legacyTargetProperty;

        private void OnEnable()
        {
            navigatorProperty = serializedObject.FindProperty("navigator");
            targetNodeIdProperty = serializedObject.FindProperty("targetNodeId");
            legacyTargetProperty = serializedObject.FindProperty("legacyTarget");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(navigatorProperty);
            DrawTargetPopup();

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(legacyTargetProperty);
            EditorGUILayout.HelpBox("Bind Button.onClick to PushTarget, Back, or ToRoot. PushTarget uses Target Node Id first, then falls back to Legacy Target.", MessageType.Info);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawTargetPopup()
        {
            var navigator = navigatorProperty.objectReferenceValue as UINavigator;
            if (navigator == null)
            {
                navigator = ((UINavButton)target).GetComponentInParent<UINavigator>();
            }

            if (navigator == null)
            {
                navigator = UnityEngine.Object.FindObjectOfType<UINavigator>();
            }

            var root = navigator != null ? navigator.NavigationRoot : UnityEngine.Object.FindObjectOfType<UINavigationRoot>();
            if (root == null)
            {
                root = ((UINavButton)target).GetComponentInParent<UINavigationRoot>();
            }

            if (root == null || root.Entries.Count == 0)
            {
                EditorGUILayout.PropertyField(targetNodeIdProperty);
                EditorGUILayout.HelpBox("No UINavigationRoot with entries found. Assign Target Node Id manually or configure a root.", MessageType.Warning);
                return;
            }

            var labels = new List<string> { "None" };
            var ids = new List<string> { string.Empty };

            foreach (var entry in root.Entries)
            {
                if (entry == null)
                {
                    continue;
                }

                labels.Add(entry.DisplayName);
                ids.Add(entry.Id);
            }

            int currentIndex = ids.IndexOf(targetNodeIdProperty.stringValue);
            if (currentIndex < 0)
            {
                currentIndex = 0;
            }

            int nextIndex = EditorGUILayout.Popup("Target Node", currentIndex, labels.ToArray());
            targetNodeIdProperty.stringValue = ids[nextIndex];
        }
    }
}
