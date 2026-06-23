using System;
using Core.Runtime;
using Core.Table;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Core.Examples
{
    /// <summary>
    /// GameDataCenter / GameEventBus / ExcelTableLoader 的完整使用示例。
    ///
    /// 推荐你这样看：
    /// 1. 先看 Awake：怎么初始化默认数值
    /// 2. 再看 OnEnable：UI 怎么监听数值和事件
    /// 3. 再看下面几个 public 方法：按钮、礼物、提案怎么改数据
    /// 4. 最后看 ReadWholeGiftTable：Excel 怎么读取整张表
    ///
    /// 使用方法：
    /// - 在场景里建一个空物体
    /// - 挂上这个脚本
    /// - 可选：把 UI Text / Slider 拖到 Inspector 对应字段
    /// - 播放后按 Space，可以看到按钮事件日志
    ///
    /// Excel 表格示例：
    ///
    /// 文件位置：
    /// Assets/StreamingAssets/GiftConfigExample.xlsx
    ///
    /// Sheet 名：
    /// 礼物表
    ///
    /// 按 T 键可以读取整张礼物表并打印到 Console。
    /// </summary>
    public sealed class GameDataUsageExample : MonoBehaviour
    {
        [Header("可选 UI 绑定")]
        [SerializeField] private TextMeshProUGUI supportRateText;
        [SerializeField] private TextMeshProUGUI deterrenceText;
        [SerializeField] private TextMeshProUGUI heartRateText;
        [SerializeField] private Slider supportRateSlider;
        [SerializeField] private Slider deterrenceSlider;
        [SerializeField] private Slider heartRateSlider;

        [Header("可选 Excel 表格读取示例")]
        [Tooltip("文件需要放到 Assets/StreamingAssets/ 下面。")]
        [SerializeField] private string excelFileName = "GiftConfigExample.xlsx";
        [SerializeField] private string giftSheetName = "礼物表";
        [SerializeField] private bool readWholeGiftTableOnStart;

        private IDisposable _supportRateListener;
        private IDisposable _deterrenceListener;
        private IDisposable _heartRateListener;
        private IDisposable _buttonEventListener;
        private IDisposable _giftEventListener;

        private void Awake()
        {
            RegisterDefaultValuesByCode();

            if (readWholeGiftTableOnStart)
            {
                ReadWholeGiftTable();
            }
        }

        private void OnEnable()
        {
            // 监听单个数值。
            // replayCurrentValue=true 表示刚订阅时会立刻收到一次当前值，
            // 所以 UI 一打开就能显示正确数字。
            _supportRateListener = GameDataCenter.Observe(GameDataKeys.SupportRate, OnSupportRateChanged, true);
            _deterrenceListener = GameDataCenter.Observe(GameDataKeys.Deterrence, OnDeterrenceChanged, true);
            _heartRateListener = GameDataCenter.Observe(GameDataKeys.HeartRate, OnHeartRateChanged, true);

            // 监听离散事件。
            _buttonEventListener = GameEventBus.Observe(GameEventNames.ButtonPressed, OnButtonPressed);
            _giftEventListener = GameEventBus.Observe(GameEventNames.GiftReceived, OnGiftReceivedEvent);
        }

        private void OnDisable()
        {
            // 只要订阅了，就要在对象关闭/销毁时取消订阅。
            // 不取消的话，物体没了还可能继续收到事件。
            _supportRateListener?.Dispose();
            _deterrenceListener?.Dispose();
            _heartRateListener?.Dispose();
            _buttonEventListener?.Dispose();
            _giftEventListener?.Dispose();
        }

        /// <summary>
        /// 不读 Excel 时，用代码直接注册默认值。
        /// 这是最简单、最稳的用法。
        /// </summary>
        private void RegisterDefaultValuesByCode()
        {
            GameDataCenter.RegisterDefault(GameDataKeys.SupportRate, 50f);
            GameDataCenter.RegisterDefault(GameDataKeys.Deterrence, 0f);
            GameDataCenter.RegisterDefault(GameDataKeys.HeartRate, 70f);
        }

        /// <summary>
        /// 示例 1：玩家按下“不要按”的按钮。
        /// 这个方法可以绑定到 Unity Button 的 OnClick。
        /// </summary>
        public void ClickDangerButton()
        {
            GameDataCenter.Add(GameDataKeys.SupportRate, -10f, GameChangeSource.PlayerAction);
            GameDataCenter.Add(GameDataKeys.HeartRate, 15f, GameChangeSource.PlayerAction);
            GameDataCenter.Add(GameDataKeys.Deterrence, 8f, GameChangeSource.PlayerAction);

            GameDataCenter.Clamp(GameDataKeys.SupportRate, 0f, 100f);
            GameDataCenter.Clamp(GameDataKeys.HeartRate, 40f, 140f);
            GameDataCenter.Clamp(GameDataKeys.Deterrence, 0f, 100f);

            GameEventBus.Publish(GameEventNames.ButtonPressed, "danger_button", GameChangeSource.PlayerAction);
        }

        /// <summary>
        /// 示例 2：通过一个提案。
        /// </summary>
        public void ApproveProposal()
        {
            GameDataCenter.Add(GameDataKeys.SupportRate, 5f, GameChangeSource.Proposal);
            GameDataCenter.Add(GameDataKeys.HeartRate, 3f, GameChangeSource.Proposal);
            GameEventBus.Publish(GameEventNames.ProposalResolved, "approve", GameChangeSource.Proposal);
        }

        /// <summary>
        /// 示例 3：否决一个提案。
        /// </summary>
        public void RejectProposal()
        {
            GameDataCenter.Add(GameDataKeys.SupportRate, -3f, GameChangeSource.Proposal);
            GameDataCenter.Add(GameDataKeys.Deterrence, 2f, GameChangeSource.Proposal);
            GameEventBus.Publish(GameEventNames.ProposalResolved, "reject", GameChangeSource.Proposal);
        }

        /// <summary>
        /// 示例 4：收到礼物。
        /// 以后读表后，这里的数字可以来自效果表。
        /// </summary>
        public void ReceiveGift()
        {
            var giftId = "G_1";

            GameDataCenter.Add(GameDataKeys.SupportRate, 5f, GameChangeSource.Gift);
            GameDataCenter.Add(GameDataKeys.HeartRate, -2f, GameChangeSource.Gift);
            GameEventBus.Publish(GameEventNames.GiftReceived, giftId, GameChangeSource.Gift);
        }

        /// <summary>
        /// 示例 5：重新开始游戏。
        /// </summary>
        public void RestartGame()
        {
            GameDataCenter.ResetAll();
            GameEventBus.Publish(GameEventNames.GameStarted, null, GameChangeSource.Restore);
        }

        private void Update()
        {
            // 键盘测试：
            // Space = 按按钮
            // A = 通过提案
            // R = 否决提案
            // G = 收礼物
            // T = 读取整张礼物表
            // Backspace = 重开
            if (Input.GetKeyDown(KeyCode.Space))
            {
                ClickDangerButton();
            }

            if (Input.GetKeyDown(KeyCode.A))
            {
                ApproveProposal();
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                RejectProposal();
            }

            if (Input.GetKeyDown(KeyCode.G))
            {
                ReceiveGift();
            }

            if (Input.GetKeyDown(KeyCode.T))
            {
                ReadWholeGiftTable();
            }

            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                RestartGame();
            }
        }

        /// <summary>
        /// 读取整张礼物表。
        ///
        /// 这个方法只是演示“Excel 确实能读出来”，不会把表格内容注册进 GameDataCenter。
        ///
        /// 文件：
        /// Assets/StreamingAssets/GiftConfigExample.xlsx
        ///
        /// Sheet：
        /// 礼物表
        ///
        /// 读出来后会逐行打印：
        /// 第 1 行：英文代码字段名
        /// 第 2 行：中文说明
        /// 第 3 行：类型说明
        /// 第 4 行开始：真正礼物数据
        /// </summary>
        public void ReadWholeGiftTable()
        {
            var path = System.IO.Path.Combine(Application.streamingAssetsPath, excelFileName);
            var table = ExcelTableLoader.LoadRawSheet(path, giftSheetName);

            Debug.Log($"开始读取整张表：{excelFileName} / {giftSheetName}，共 {table.Count} 行");

            for (var rowIndex = 0; rowIndex < table.Count; rowIndex++)
            {
                var row = table[rowIndex];
                var line = string.Join(" | ", row);
                Debug.Log($"第 {rowIndex + 1} 行：{line}");
            }
        }

        private void OnSupportRateChanged(GameDataChangedEvent change)
        {
            SetText(supportRateText, $"支持率: {change.NewValue:0}");
            SetSlider(supportRateSlider, change.NewValue, 0f, 100f);
        }

        private void OnDeterrenceChanged(GameDataChangedEvent change)
        {
            SetText(deterrenceText, $"威慑值: {change.NewValue:0}");
            SetSlider(deterrenceSlider, change.NewValue, 0f, 100f);
        }

        private void OnHeartRateChanged(GameDataChangedEvent change)
        {
            SetText(heartRateText, $"心率: {change.NewValue:0}");
            SetSlider(heartRateSlider, change.NewValue, 40f, 140f);
        }

        private void OnButtonPressed(GameEvent gameEvent)
        {
            Debug.Log($"[事件] 按钮被按下，按钮ID={gameEvent.Payload}");
        }

        private void OnGiftReceivedEvent(GameEvent gameEvent)
        {
            Debug.Log($"[事件] 收到礼物，礼物ID={gameEvent.Payload}");
        }

        private static void SetText(TextMeshProUGUI target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }

        private static void SetSlider(Slider target, float value, float minValue, float maxValue)
        {
            if (target == null)
            {
                return;
            }

            target.minValue = minValue;
            target.maxValue = maxValue;
            target.value = value;
        }
    }

}
