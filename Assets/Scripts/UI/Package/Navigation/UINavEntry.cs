using System;
using System.Collections.Generic;
using UnityEngine;

namespace CWGame
{
    [Serializable]
    public class UINavEntry
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private UIBase page;
        [SerializeField] private GameObject viewRoot;
        [SerializeField] private bool closeParentOnPush = true;
        [SerializeField] private List<string> children = new List<string>();

        public string Id
        {
            get => id;
            set => id = value;
        }

        public string DisplayName
        {
            get => displayName;
            set => displayName = value;
        }

        public UIBase Page
        {
            get => page;
            set => page = value;
        }

        public GameObject ViewRoot
        {
            get => viewRoot;
            set => viewRoot = value;
        }

        public bool CloseParentOnPush
        {
            get => closeParentOnPush;
            set => closeParentOnPush = value;
        }

        public List<string> Children => children;

        public void InitializeFromPage(UIBase sourcePage)
        {
            page = sourcePage;
            if (sourcePage != null)
            {
                displayName = sourcePage.name;
                viewRoot = sourcePage.gameObject;
            }
        }

        public void EnsureId()
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                id = Guid.NewGuid().ToString("N");
            }
        }

        public void EnsureDisplayName()
        {
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                return;
            }

            if (page != null)
            {
                displayName = page.name;
                return;
            }

            if (viewRoot != null)
            {
                displayName = viewRoot.name;
                return;
            }

            displayName = "Unnamed UI";
        }

        public bool HasChild(string childId)
        {
            return !string.IsNullOrWhiteSpace(childId) && children.Contains(childId);
        }

        public void Open()
        {
            if (viewRoot != null)
            {
                if (page != null && page.gameObject == viewRoot)
                {
                    page.OnNavigateIn();
                }
                else
                {
                    viewRoot.SetActive(true);
                }

                return;
            }

            if (page != null)
            {
                page.OnNavigateIn();
            }
        }

        public void Close()
        {
            if (viewRoot != null)
            {
                if (page != null && page.gameObject == viewRoot)
                {
                    page.OnNavigateOut();
                }
                else
                {
                    viewRoot.SetActive(false);
                }

                return;
            }

            if (page != null)
            {
                page.OnNavigateOut();
            }
        }
    }
}
