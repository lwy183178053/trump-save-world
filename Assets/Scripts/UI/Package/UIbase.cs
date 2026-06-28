using UnityEngine;

namespace CWGame
{
    public class UIBase : MonoBehaviour
    {
        public int ID;
        public int Layer; // UI层级，数值越大越靠前

        public bool IsOpen { get; private set; }
        // public virtual void Awake()
        // {
        //     UIManager.Instance.RegisterPanel(this);
        // }
        // public virtual void OnDestroy()
        // {
        //     UIManager.Instance.UnRegisterPanel(this);
        // }

        public virtual void Open()
        {
            IsOpen = true;
            gameObject.SetActive(true);
        }

        public virtual void Close()
        {
            IsOpen = false;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 被导航进入时调用（Push到栈顶）
        /// </summary>
        public virtual void OnNavigateIn()
        {
            Open();
        }

        /// <summary>
        /// 被导航离开时调用（新面板Push覆盖，或Pop回退）
        /// </summary>
        public virtual void OnNavigateOut()
        {
            Close();
        }
    }
}
