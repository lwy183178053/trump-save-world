using System;
using System.Collections.Generic;
using UnityEngine;

namespace CWGame
{
    [DisallowMultipleComponent]
    public class UINavigationRoot : MonoBehaviour
    {
        [SerializeField] private string rootNodeId;
        [SerializeField] private List<UINavEntry> entries = new List<UINavEntry>();

        public string RootNodeId => rootNodeId;
        public IReadOnlyList<UINavEntry> Entries => entries;

        private void OnValidate()
        {
            EnsureIntegrity();
        }

        public void SetRoot(string nodeId)
        {
            if (FindEntry(nodeId) == null)
            {
                Debug.LogWarning($"[UINavigationRoot] Cannot set missing root node: {nodeId}", this);
                return;
            }

            rootNodeId = nodeId;
        }

        public UINavEntry FindEntry(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
            {
                return null;
            }

            foreach (var entry in entries)
            {
                if (entry != null && entry.Id == nodeId)
                {
                    return entry;
                }
            }

            return null;
        }

        public UINavEntry FindEntryByPage(UIBase page)
        {
            if (page == null)
            {
                return null;
            }

            foreach (var entry in entries)
            {
                if (entry != null && entry.Page == page)
                {
                    return entry;
                }
            }

            return null;
        }

        public UINavEntry AddPage(UIBase page)
        {
            if (page == null)
            {
                return null;
            }

            var existing = FindEntryByPage(page);
            if (existing != null)
            {
                return existing;
            }

            var entry = new UINavEntry();
            entry.InitializeFromPage(page);
            entry.EnsureId();
            entry.EnsureDisplayName();
            entries.Add(entry);

            if (string.IsNullOrWhiteSpace(rootNodeId))
            {
                rootNodeId = entry.Id;
            }

            return entry;
        }

        public UINavEntry AddEmptyEntry()
        {
            var entry = new UINavEntry();
            entry.EnsureId();
            entry.EnsureDisplayName();
            entries.Add(entry);

            if (string.IsNullOrWhiteSpace(rootNodeId))
            {
                rootNodeId = entry.Id;
            }

            return entry;
        }

        public void RemoveEntry(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
            {
                return;
            }

            entries.RemoveAll(entry => entry != null && entry.Id == nodeId);
            foreach (var entry in entries)
            {
                entry?.Children.RemoveAll(childId => childId == nodeId);
            }

            if (rootNodeId == nodeId)
            {
                rootNodeId = entries.Count > 0 ? entries[0].Id : string.Empty;
            }
        }

        public int ScanPagesInChildren()
        {
            int added = 0;
            var pages = GetComponentsInChildren<UIBase>(true);
            foreach (var page in pages)
            {
                if (FindEntryByPage(page) != null)
                {
                    continue;
                }

                AddPage(page);
                added++;
            }

            EnsureIntegrity();
            return added;
        }

        public Dictionary<string, UINavEntry> BuildEntryMap()
        {
            EnsureIntegrity();

            var map = new Dictionary<string, UINavEntry>();
            foreach (var entry in entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.Id))
                {
                    continue;
                }

                if (!map.ContainsKey(entry.Id))
                {
                    map.Add(entry.Id, entry);
                }
                else
                {
                    Debug.LogWarning($"[UINavigationRoot] Duplicate node id: {entry.Id}", this);
                }
            }

            return map;
        }

        public void EnsureIntegrity()
        {
            var usedIds = new HashSet<string>();
            foreach (var entry in entries)
            {
                if (entry == null)
                {
                    continue;
                }

                entry.EnsureId();
                while (!usedIds.Add(entry.Id))
                {
                    entry.Id = Guid.NewGuid().ToString("N");
                }

                entry.EnsureDisplayName();
                RemoveDuplicateChildren(entry.Children);
            }

            if (string.IsNullOrWhiteSpace(rootNodeId) || FindEntry(rootNodeId) == null)
            {
                rootNodeId = entries.Count > 0 ? entries[0].Id : string.Empty;
            }
        }

        private void RemoveDuplicateChildren(List<string> children)
        {
            if (children == null)
            {
                return;
            }

            var used = new HashSet<string>();
            for (int i = 0; i < children.Count; i++)
            {
                var childId = children[i];
                if (string.IsNullOrWhiteSpace(childId) || !used.Add(childId))
                {
                    children.RemoveAt(i);
                    i--;
                }
            }
        }
    }
}
