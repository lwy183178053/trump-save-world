using System.Collections.Generic;
using UnityEngine;

namespace CWGame.Editor
{
    public static class UINavigationValidator
    {
        public static List<string> Validate(UINavigationRoot root)
        {
            var problems = new List<string>();
            if (root == null)
            {
                problems.Add("No UINavigationRoot selected.");
                return problems;
            }

            var entries = root.Entries;
            if (entries.Count == 0)
            {
                problems.Add("Navigation tree has no entries.");
                return problems;
            }

            var ids = new HashSet<string>();
            var pageRefs = new HashSet<UIBase>();
            var childParentCount = new Dictionary<string, int>();

            foreach (var entry in entries)
            {
                if (entry == null)
                {
                    problems.Add("Tree contains a null entry.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(entry.Id))
                {
                    problems.Add($"{entry.DisplayName} has an empty id.");
                }
                else if (!ids.Add(entry.Id))
                {
                    problems.Add($"{entry.DisplayName} has a duplicate id.");
                }

                if (entry.Page == null && entry.ViewRoot == null)
                {
                    problems.Add($"{entry.DisplayName} has no page or view root.");
                }

                if (entry.Page != null && !pageRefs.Add(entry.Page))
                {
                    problems.Add($"{entry.DisplayName} uses a page referenced by another entry.");
                }

                foreach (var childId in entry.Children)
                {
                    if (string.IsNullOrWhiteSpace(childId))
                    {
                        problems.Add($"{entry.DisplayName} contains an empty child reference.");
                        continue;
                    }

                    if (childId == entry.Id)
                    {
                        problems.Add($"{entry.DisplayName} cannot be its own child.");
                    }

                    if (!childParentCount.ContainsKey(childId))
                    {
                        childParentCount.Add(childId, 0);
                    }

                    childParentCount[childId]++;
                }
            }

            if (string.IsNullOrWhiteSpace(root.RootNodeId))
            {
                problems.Add("Root node is not assigned.");
            }
            else if (root.FindEntry(root.RootNodeId) == null)
            {
                problems.Add("Root node id does not point to an existing entry.");
            }

            foreach (var entry in entries)
            {
                if (entry == null)
                {
                    continue;
                }

                foreach (var childId in entry.Children)
                {
                    if (root.FindEntry(childId) == null)
                    {
                        problems.Add($"{entry.DisplayName} references a missing child id.");
                    }
                }
            }

            foreach (var pair in childParentCount)
            {
                if (pair.Value > 1)
                {
                    var child = root.FindEntry(pair.Key);
                    var childName = child != null ? child.DisplayName : pair.Key;
                    problems.Add($"{childName} has more than one parent.");
                }
            }

            if (HasCycle(root))
            {
                problems.Add("Navigation tree contains a cycle.");
            }

            foreach (var page in root.GetComponentsInChildren<UIBase>(true))
            {
                if (root.FindEntryByPage(page) == null)
                {
                    problems.Add($"{page.name} exists under the root but is not in the navigation tree.");
                }
            }

            return problems;
        }

        private static bool HasCycle(UINavigationRoot root)
        {
            var visiting = new HashSet<string>();
            var visited = new HashSet<string>();

            foreach (var entry in root.Entries)
            {
                if (entry == null)
                {
                    continue;
                }

                if (Visit(root, entry.Id, visiting, visited))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool Visit(UINavigationRoot root, string id, HashSet<string> visiting, HashSet<string> visited)
        {
            if (visited.Contains(id))
            {
                return false;
            }

            if (!visiting.Add(id))
            {
                return true;
            }

            var entry = root.FindEntry(id);
            if (entry != null)
            {
                foreach (var childId in entry.Children)
                {
                    if (Visit(root, childId, visiting, visited))
                    {
                        return true;
                    }
                }
            }

            visiting.Remove(id);
            visited.Add(id);
            return false;
        }
    }
}
