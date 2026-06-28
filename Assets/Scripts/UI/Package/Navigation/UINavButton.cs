using UnityEngine;

namespace CWGame
{
    [DisallowMultipleComponent]
    public class UINavButton : MonoBehaviour
    {
        [Header("导航器配置")]
        [SerializeField] private UINavigator navigator; // 导航按钮的导航器组件，如果没有设置，将在运行时尝试自动查找。

        [Header("目标节点配置")]
        [SerializeField] private string targetNodeId; // 导航按钮的目标节点ID。

        public string TargetNodeId
        {
            get => targetNodeId;
            set => targetNodeId = value;
        }

        public void PushTarget()
        {
            var nav = ResolveNavigator();
            if (nav == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(targetNodeId))
            {
                nav.Push(targetNodeId);
            }
        }

        public void Back()
        {
            var nav = ResolveNavigator();
            if (nav == null)
            {
                return;
            }

            nav.Back();
        }

        public void ToRoot()
        {
            var nav = ResolveNavigator();
            if (nav == null)
            {
                return;
            }

            nav.ResetToRoot();
        }

        private UINavigator ResolveNavigator()
        {
            if (navigator != null)
            {
                return navigator;
            }

            if (UINavigator.Instance != null)
            {
                return UINavigator.Instance;
            }

            navigator = FindObjectOfType<UINavigator>();
            if (navigator == null)
            {
                Debug.LogWarning("[UINavButton] No UINavigator found in the scene.", this);
            }

            return navigator;
        }
    }
}
