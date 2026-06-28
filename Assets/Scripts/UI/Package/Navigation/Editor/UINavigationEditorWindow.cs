using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CWGame.Editor
{
    public class UINavigationEditorWindow : EditorWindow
    {
        private UINavigationRoot root;
        private string selectedNodeId;
        private Vector2 nodeScroll;
        private Vector2 detailScroll;
        private Vector2 problemScroll;
        private readonly Dictionary<string, int> addChildSelectionByParent = new Dictionary<string, int>();
        private readonly HashSet<string> expandedNodeIds = new HashSet<string>();

        [MenuItem("Tools/CWGame/UI Navigation Editor")]
        public static void Open()
        {
            GetWindow<UINavigationEditorWindow>("UI Navigation");
        }

        public static void Open(UINavigationRoot selectedRoot)
        {
            var window = GetWindow<UINavigationEditorWindow>("UI Navigation");
            window.root = selectedRoot;
            window.selectedNodeId = selectedRoot != null ? selectedRoot.RootNodeId : string.Empty;
            window.Show();
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (root == null)
            {
                EditorGUILayout.HelpBox("Select a UINavigationRoot to edit.", MessageType.Info);
                return;
            }

            root.EnsureIntegrity();
            if (string.IsNullOrWhiteSpace(selectedNodeId) || root.FindEntry(selectedNodeId) == null)
            {
                selectedNodeId = root.RootNodeId;
            }

            if (!string.IsNullOrWhiteSpace(root.RootNodeId) && expandedNodeIds.Count == 0)
            {
                expandedNodeIds.Add(root.RootNodeId);
            }

            EditorGUILayout.BeginHorizontal();
            DrawNavigationTree();
            DrawDetails();
            EditorGUILayout.EndHorizontal();

            DrawProblems();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            root = (UINavigationRoot)EditorGUILayout.ObjectField(root, typeof(UINavigationRoot), true, GUILayout.MinWidth(220));

            if (GUILayout.Button("Use Selection", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                var selected = Selection.activeGameObject != null
                    ? Selection.activeGameObject.GetComponent<UINavigationRoot>()
                    : null;

                if (selected != null)
                {
                    root = selected;
                    selectedNodeId = root.RootNodeId;
                }
            }

            GUI.enabled = root != null;
            if (GUILayout.Button("Scan UI", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                Undo.RecordObject(root, "Scan UI Navigation Pages");
                int added = root.ScanPagesInChildren();
                EditorUtility.SetDirty(root);
                Debug.Log($"[UINavigationRoot] Added {added} UI pages.", root);
            }

            if (GUILayout.Button("Add Empty", EditorStyles.toolbarButton, GUILayout.Width(90)))
            {
                Undo.RecordObject(root, "Add UI Navigation Entry");
                var entry = root.AddEmptyEntry();
                selectedNodeId = entry.Id;
                EditorUtility.SetDirty(root);
            }

            if (GUILayout.Button("Validate", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                var problems = UINavigationValidator.Validate(root);
                Debug.Log(problems.Count == 0
                    ? "[UINavigationRoot] Navigation tree is valid."
                    : $"[UINavigationRoot] Navigation tree has {problems.Count} problem(s).", root);
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();
        }

        private void DrawNavigationTree()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(320));
            EditorGUILayout.LabelField("Navigation Tree", EditorStyles.boldLabel);

            nodeScroll = EditorGUILayout.BeginScrollView(nodeScroll, GUI.skin.box, GUILayout.MinHeight(300));

            var rootEntry = root.FindEntry(root.RootNodeId);
            if (rootEntry != null)
            {
                DrawTreeEntry(rootEntry, 0, new HashSet<string>());
            }
            else
            {
                EditorGUILayout.HelpBox("Root node is missing.", MessageType.Warning);
            }

            DrawUnlinkedEntries();
            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Set Root"))
            {
                Undo.RecordObject(root, "Set UI Navigation Root");
                root.SetRoot(selectedNodeId);
                expandedNodeIds.Add(selectedNodeId);
                EditorUtility.SetDirty(root);
            }

            if (GUILayout.Button("Delete"))
            {
                DeleteSelected();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawTreeEntry(UINavEntry entry, int depth, HashSet<string> visited)
        {
            if (entry == null)
            {
                return;
            }

            if (!visited.Add(entry.Id))
            {
                DrawIndentedLabel(depth, $"{entry.DisplayName}  (Cycle)", MessageType.Warning);
                return;
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(depth * 18);

            if (entry.Children.Count > 0)
            {
                bool expanded = expandedNodeIds.Contains(entry.Id);
                var foldoutRect = GUILayoutUtility.GetRect(16, EditorGUIUtility.singleLineHeight, GUILayout.Width(16));
                bool nextExpanded = EditorGUI.Foldout(foldoutRect, expanded, GUIContent.none, true);
                if (nextExpanded)
                {
                    expandedNodeIds.Add(entry.Id);
                }
                else
                {
                    expandedNodeIds.Remove(entry.Id);
                }
            }
            else
            {
                GUILayout.Space(16);
            }

            var label = BuildTreeLabel(entry, depth);
            var isSelected = selectedNodeId == entry.Id;
            if (GUILayout.Toggle(isSelected, label, "Button") != isSelected)
            {
                selectedNodeId = entry.Id;
                GUI.FocusControl(null);
            }

            EditorGUILayout.EndHorizontal();

            if (entry.Children.Count > 0 && expandedNodeIds.Contains(entry.Id))
            {
                foreach (var childId in entry.Children)
                {
                    var child = root.FindEntry(childId);
                    if (child == null)
                    {
                        DrawIndentedLabel(depth + 1, "Missing Child", MessageType.Warning);
                        continue;
                    }

                    DrawTreeEntry(child, depth + 1, new HashSet<string>(visited));
                }
            }
        }

        private string BuildTreeLabel(UINavEntry entry, int depth)
        {
            var label = $"[L{depth}] {entry.DisplayName}";
            if (root.RootNodeId == entry.Id)
            {
                label += "  (Root)";
            }

            if (entry.Children.Count > 0)
            {
                label += $"  ({entry.Children.Count})";
            }

            return label;
        }

        private void DrawUnlinkedEntries()
        {
            var linkedIds = BuildLinkedIdSet();
            bool hasUnlinked = false;

            foreach (var entry in root.Entries)
            {
                if (entry != null && !linkedIds.Contains(entry.Id))
                {
                    hasUnlinked = true;
                    break;
                }
            }

            if (!hasUnlinked)
            {
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Unlinked", EditorStyles.boldLabel);

            foreach (var entry in root.Entries)
            {
                if (entry == null || linkedIds.Contains(entry.Id))
                {
                    continue;
                }

                var isSelected = selectedNodeId == entry.Id;
                if (GUILayout.Toggle(isSelected, $"[Unlinked] {entry.DisplayName}", "Button") != isSelected)
                {
                    selectedNodeId = entry.Id;
                    GUI.FocusControl(null);
                }
            }
        }

        private HashSet<string> BuildLinkedIdSet()
        {
            var linkedIds = new HashSet<string>();
            var rootEntry = root.FindEntry(root.RootNodeId);
            CollectLinkedIds(rootEntry, linkedIds);
            return linkedIds;
        }

        private void CollectLinkedIds(UINavEntry entry, HashSet<string> linkedIds)
        {
            if (entry == null || !linkedIds.Add(entry.Id))
            {
                return;
            }

            foreach (var childId in entry.Children)
            {
                CollectLinkedIds(root.FindEntry(childId), linkedIds);
            }
        }

        private void DrawIndentedLabel(int depth, string message, MessageType type)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space((depth * 18) + 16);
            EditorGUILayout.HelpBox(message, type);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawDetails()
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Selected Node", EditorStyles.boldLabel);

            var selected = root.FindEntry(selectedNodeId);
            if (selected == null)
            {
                EditorGUILayout.HelpBox("No node selected.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            detailScroll = EditorGUILayout.BeginScrollView(detailScroll, GUI.skin.box, GUILayout.MinHeight(300));
            DrawSelectedFields(selected);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Children", EditorStyles.boldLabel);
            DrawChildren(selected);

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawSelectedFields(UINavEntry selected)
        {
            EditorGUI.BeginChangeCheck();

            var displayName = EditorGUILayout.TextField("Display Name", selected.DisplayName);
            var page = (UIBase)EditorGUILayout.ObjectField("Page", selected.Page, typeof(UIBase), true);
            var viewRoot = (GameObject)EditorGUILayout.ObjectField("View Root", selected.ViewRoot, typeof(GameObject), true);
            var closeParentOnPush = EditorGUILayout.Toggle("Close Parent On Push", selected.CloseParentOnPush);

            if (!EditorGUI.EndChangeCheck())
            {
                return;
            }

            Undo.RecordObject(root, "Edit UI Navigation Entry");
            selected.DisplayName = displayName;
            selected.Page = page;
            selected.ViewRoot = viewRoot;
            selected.CloseParentOnPush = closeParentOnPush;
            root.EnsureIntegrity();
            EditorUtility.SetDirty(root);
        }

        private void DrawChildren(UINavEntry selected)
        {
            for (int i = 0; i < selected.Children.Count; i++)
            {
                var child = root.FindEntry(selected.Children[i]);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(child != null ? BuildChildSummary(child) : "Missing Child", GUILayout.MinWidth(160));

                if (GUILayout.Button("Select", GUILayout.Width(70)) && child != null)
                {
                    selectedNodeId = child.Id;
                    GUI.FocusControl(null);
                }

                if (GUILayout.Button("Remove", GUILayout.Width(80)))
                {
                    Undo.RecordObject(root, "Remove UI Navigation Child");
                    selected.Children.RemoveAt(i);
                    EditorUtility.SetDirty(root);
                    EditorGUILayout.EndHorizontal();
                    return;
                }

                EditorGUILayout.EndHorizontal();
            }

            var options = BuildChildOptions(selected, out var ids);
            if (ids.Count == 0)
            {
                EditorGUILayout.HelpBox("No available child nodes.", MessageType.None);
                return;
            }

            EditorGUILayout.BeginHorizontal();
            int currentOption = GetAddChildSelection(selected.Id, ids.Count);
            int selectedOption = EditorGUILayout.Popup("Add Child", currentOption, options);
            addChildSelectionByParent[selected.Id] = selectedOption;

            if (GUILayout.Button("Add", GUILayout.Width(70)))
            {
                var childId = ids[selectedOption];
                if (!selected.Children.Contains(childId))
                {
                    Undo.RecordObject(root, "Add UI Navigation Child");
                    selected.Children.Add(childId);
                    expandedNodeIds.Add(selected.Id);
                    root.EnsureIntegrity();
                    EditorUtility.SetDirty(root);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private string BuildChildSummary(UINavEntry child)
        {
            int depth = GetDepthFromRoot(child.Id);
            var prefix = depth >= 0 ? $"[L{depth}]" : "[Unlinked]";
            return $"{prefix} {child.DisplayName}";
        }

        private int GetAddChildSelection(string parentId, int optionCount)
        {
            if (!addChildSelectionByParent.TryGetValue(parentId, out var selectedOption))
            {
                return 0;
            }

            if (selectedOption < 0 || selectedOption >= optionCount)
            {
                return 0;
            }

            return selectedOption;
        }

        private string[] BuildChildOptions(UINavEntry parent, out List<string> ids)
        {
            ids = new List<string>();
            var labels = new List<string>();
            var ancestors = BuildAncestorSet(parent.Id);
            var linkedIds = BuildLinkedIdSet();

            foreach (var entry in root.Entries)
            {
                if (entry == null || entry.Id == parent.Id || parent.Children.Contains(entry.Id) || ancestors.Contains(entry.Id))
                {
                    continue;
                }

                ids.Add(entry.Id);
                labels.Add(BuildChildOptionLabel(entry, linkedIds));
            }

            return labels.ToArray();
        }

        private string BuildChildOptionLabel(UINavEntry entry, HashSet<string> linkedIds)
        {
            var depth = GetDepthFromRoot(entry.Id);
            if (depth >= 0)
            {
                return $"[L{depth}] {entry.DisplayName}";
            }

            return linkedIds.Contains(entry.Id) ? entry.DisplayName : $"[Unlinked] {entry.DisplayName}";
        }

        private HashSet<string> BuildAncestorSet(string nodeId)
        {
            var ancestors = new HashSet<string>();
            BuildAncestorSet(nodeId, ancestors, new HashSet<string>());
            return ancestors;
        }

        private void BuildAncestorSet(string nodeId, HashSet<string> ancestors, HashSet<string> visited)
        {
            if (!visited.Add(nodeId))
            {
                return;
            }

            foreach (var entry in root.Entries)
            {
                if (entry != null && entry.Children.Contains(nodeId))
                {
                    ancestors.Add(entry.Id);
                    BuildAncestorSet(entry.Id, ancestors, visited);
                }
            }
        }

        private int GetDepthFromRoot(string nodeId)
        {
            var rootEntry = root.FindEntry(root.RootNodeId);
            return GetDepthRecursive(rootEntry, nodeId, 0, new HashSet<string>());
        }

        private int GetDepthRecursive(UINavEntry entry, string nodeId, int depth, HashSet<string> visited)
        {
            if (entry == null || !visited.Add(entry.Id))
            {
                return -1;
            }

            if (entry.Id == nodeId)
            {
                return depth;
            }

            foreach (var childId in entry.Children)
            {
                var found = GetDepthRecursive(root.FindEntry(childId), nodeId, depth + 1, visited);
                if (found >= 0)
                {
                    return found;
                }
            }

            return -1;
        }

        private void DrawProblems()
        {
            var problems = UINavigationValidator.Validate(root);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Problems", EditorStyles.boldLabel);

            problemScroll = EditorGUILayout.BeginScrollView(problemScroll, GUI.skin.box, GUILayout.Height(130));
            if (problems.Count == 0)
            {
                EditorGUILayout.HelpBox("Navigation tree is valid.", MessageType.Info);
            }
            else
            {
                foreach (var problem in problems)
                {
                    EditorGUILayout.HelpBox(problem, MessageType.Warning);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void DeleteSelected()
        {
            if (string.IsNullOrWhiteSpace(selectedNodeId))
            {
                return;
            }

            if (!EditorUtility.DisplayDialog("Delete UI Navigation Node", "Delete the selected node from the navigation tree?", "Delete", "Cancel"))
            {
                return;
            }

            Undo.RecordObject(root, "Delete UI Navigation Entry");
            var deletedNodeId = selectedNodeId;
            root.RemoveEntry(selectedNodeId);
            selectedNodeId = root.RootNodeId;
            addChildSelectionByParent.Remove(deletedNodeId);
            expandedNodeIds.Remove(deletedNodeId);
            EditorUtility.SetDirty(root);
        }
    }
}
