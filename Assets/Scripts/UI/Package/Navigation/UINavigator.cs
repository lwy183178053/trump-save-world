using System.Collections.Generic;
using UnityEngine;

namespace CWGame
{
    [DisallowMultipleComponent]
    public class UINavigator : MonoBehaviour
    {
        public static UINavigator Instance { get; private set; }

        [Header("Root Configuration")]
        [SerializeField] private UINavigationRoot navigationRoot;
        [SerializeField] private bool openRootOnStart = true;
        [SerializeField] private bool closeAllEntriesOnAwake = true;
        [SerializeField] private bool restrictToDirectChildren = true;

        private readonly Stack<string> navStack = new Stack<string>();
        private Dictionary<string, UINavEntry> entryById = new Dictionary<string, UINavEntry>();

        public string CurrentNodeId => navStack.Count > 0 ? navStack.Peek() : string.Empty;
        public UINavEntry Current => GetEntry(CurrentNodeId);
        public UINavigationRoot NavigationRoot
        {
            get
            {
                ResolveNavigationRoot();
                return navigationRoot;
            }
        }

        private bool HasRootConfig => navigationRoot != null && navigationRoot.Entries.Count > 0;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[UINavigator] More than one navigator exists in the scene.");
            }

            Instance = this;
            ResolveNavigationRoot();
            RebuildCache();

            if (closeAllEntriesOnAwake)
            {
                CloseAll();
            }
        }

        private void Start()
        {
            if (openRootOnStart)
            {
                ResetToRoot();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void RebuildCache()
        {
            if (HasRootConfig)
            {
                entryById = navigationRoot.BuildEntryMap();
            }
        }

        public bool Push(string targetNodeId)
        {
            RebuildCache();

            var target = GetEntry(targetNodeId);
            if (target == null)
            {
                Debug.LogWarning($"[UINavigator] Cannot push missing node: {targetNodeId}", this);
                return false;
            }

            if (CurrentNodeId == targetNodeId)
            {
                return true;
            }

            if (navStack.Count == 0)
            {
                navStack.Push(targetNodeId);
                target.Open();
                return true;
            }

            var current = Current;
            if (restrictToDirectChildren && current != null && !current.HasChild(targetNodeId))
            {
                Debug.LogWarning($"[UINavigator] {target.DisplayName} is not a direct child of {current.DisplayName}.", this);
                return false;
            }

            if (current != null && current.CloseParentOnPush)
            {
                current.Close();
            }

            navStack.Push(targetNodeId);
            target.Open();
            return true;
        }

        public bool Push(UINavEntry target)
        {
            return target != null && Push(target.Id);
        }

        public bool Back()
        {
            if (navStack.Count <= 1)
            {
                Debug.LogWarning("[UINavigator] Navigation stack is already at root.", this);
                return false;
            }

            var current = GetEntry(navStack.Pop());
            current?.Close();

            Current?.Open();
            return true;
        }

        public void ResetToRoot()
        {
            RebuildCache();

            if (GetEntry(navigationRoot.RootNodeId) == null)
            {
                Debug.LogWarning("[UINavigator] Root node is not assigned.", this);
                return;
            }

            while (navStack.Count > 0)
            {
                GetEntry(navStack.Pop())?.Close();
            }

            navStack.Push(navigationRoot.RootNodeId);
            Current?.Open();
        }

        public bool Replace(string targetNodeId)
        {
            RebuildCache();

            var target = GetEntry(targetNodeId);
            if (target == null)
            {
                Debug.LogWarning($"[UINavigator] Cannot replace with missing node: {targetNodeId}", this);
                return false;
            }

            if (navStack.Count > 0 && restrictToDirectChildren && !Current.HasChild(targetNodeId))
            {
                Debug.LogWarning($"[UINavigator] {target.DisplayName} is not a direct child of {Current.DisplayName}.", this);
                return false;
            }

            if (navStack.Count > 0)
            {
                GetEntry(navStack.Pop())?.Close();
            }

            navStack.Push(targetNodeId);
            target.Open();
            return true;
        }

        private UINavEntry GetEntry(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
            {
                return null;
            }

            return entryById.TryGetValue(nodeId, out var entry) ? entry : null;
        }

        private void ResolveNavigationRoot()
        {
            if (navigationRoot != null)
            {
                return;
            }

            navigationRoot = GetComponent<UINavigationRoot>();
            if (navigationRoot != null)
            {
                return;
            }

            navigationRoot = GetComponentInParent<UINavigationRoot>();
            if (navigationRoot != null)
            {
                return;
            }

            navigationRoot = FindObjectOfType<UINavigationRoot>();
        }

        private void CloseAll()
        {
            if (HasRootConfig)
            {
                foreach (var entry in entryById.Values)
                {
                    entry?.Close();
                }
            }
        }
    }
}
