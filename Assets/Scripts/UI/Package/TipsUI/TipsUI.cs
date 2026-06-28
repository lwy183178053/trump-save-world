using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CWGame
{
    /// <summary>
    /// 提示型UI（TipUI）- 单角色线性对话展示，适合教程提示、系统引导等
    /// 特点：一次展示一段话，点击/按任意键推进下一段，无分支逻辑
    /// </summary>
    public class TipsUI : UIBase
    {
        [SerializeField]
        [Tooltip("角色立绘")]
        private Image CharacterUI;
        [SerializeField]
        private TextMeshProUGUI CharacterNameText;
        [SerializeField]
        [Tooltip("跳过按钮")]
        private Button SkipButton;
        [SerializeField]
        [Tooltip("对话框")]
        private Image DialogBox;
        [SerializeField]
        [Tooltip("提示文本")]
        private TextMeshProUGUI TipText;
        [SerializeField]
        [Tooltip("提示期间需要禁用的操作组件（如玩家控制器）")]
        private MonoBehaviour operationComponent;

        private int currentNodeIndex = 0;
        private TipNode currentNode;
        private TipSO tipSO;

        void OnDestroy()
        {
            TipEvents.OnTipRequested -= OnTipRequested;
        }

        void Start()
        {
            Close();
            TipEvents.OnTipRequested += OnTipRequested;
        }

        private void OnTipRequested(TipSO tipSO)
        {
            print("收到提示请求");
            if (tipSO == null || tipSO.nodes.Length == 0)
            {
                Debug.LogWarning("提示SO为空");
                return;
            }

            this.tipSO = tipSO;
            currentNodeIndex = 0;
            Open();
            ShowNode(currentNodeIndex);

            // 通知其他系统提示已开始，禁用玩家操作
            TipEvents.TriggerTipStart(currentNode);
            if (operationComponent != null)
                operationComponent.enabled = false;
        }

        private void ShowNode(int index)
        {
            if (tipSO == null || index < 0 || index >= tipSO.nodes.Length)
                return;

            currentNodeIndex = index;
            currentNode = tipSO.nodes[index];

            UpdateTip(index);

            TipEvents.TriggerTipNodeUpdate(index, currentNode);
        }

        private void UpdateTip(int index)
        {
            if (currentNode == null)
                return;

            CharacterUI.sprite = currentNode.CharacterSprite ?? null; // 如果没有立绘就不显示
            CharacterNameText.text = currentNode.CharacterName ?? string.Empty; // 如果没有角色名就不显示
            ///普通提示也可以用，就不放角色立绘和角色名就行了，这样就只显示文字
            TipText.text = currentNode.DialogueText;

            SkipButton.gameObject.SetActive(!currentNode.IsEnd);
        }

        public void NextTip()
        {
            if (tipSO == null)
                return;

            int nextIndex = currentNodeIndex + 1;
            if (nextIndex >= tipSO.nodes.Length)
            {
                EndTip();
                return;
            }

            ShowNode(nextIndex);

            if (tipSO.nodes[nextIndex].IsEnd)
            {
                EndTip();
            }
        }

        private void EndTip()
        {
            if (operationComponent != null)
                operationComponent.enabled = true;
            TipEvents.TriggerTipEnd();
            Close();
        }

        public void SkipTip()
        {
            if (tipSO == null || currentNodeIndex >= tipSO.nodes.Length - 1)
                return;

            ShowNode(tipSO.nodes.Length - 1);
            EndTip();
        }

        void Update()
        {
            TipUIOperation();
        }

        void TipUIOperation()
        {
            
                if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
                {
                    print("点击了提示UI");
                    NextTip();
                }
            
        }
    }
}
