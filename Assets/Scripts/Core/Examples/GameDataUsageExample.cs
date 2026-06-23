using System;
using System.Collections.Generic;
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
    /// 4. 最后看 LoadVariableConfigFromExcel：Excel 怎么读取成类
    ///
    /// 使用方法：
    /// - 在场景里建一个空物体
    /// - 挂上这个脚本
    /// - 可选：把 UI Text / Slider 拖到 Inspector 对应字段
    /// - 播放后按 Space，可以看到按钮事件日志
    ///
    /// Excel 表结构建议：
    ///
    /// Sheet 名：变量表
    ///
    /// 第一行必须是字段名，字段名要和 VariableConfigRow 类里的 public 字段一致：
    ///
    /// ID | Name | InitValue | MinValue | MaxValue | Desc
    ///
    /// 第二行开始填数据：
    ///
    /// A_1 | 支持率 | 50 | 0 | 100 | 玩家支持率
    /// D_1 | 威慑值 | 0  | 0 | 100 | 胜利进度
    /// H_1 | 心率   | 70 | 40 | 140 | 当前心率
    ///
    /// 注意：
    /// - 这个 Excel 读取器是比赛用轻量版，只支持 .xlsx
    /// - 表头建议用英文，类字段也用英文，最稳
    /// - 中文说明可以放在 Name / Desc 里
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

        [Header("可选 Excel 配置")]
        [Tooltip("例如：GameConfig.xlsx。文件建议放到 Assets/StreamingAssets/ 下面。")]
        [SerializeField] private string excelFileName = "GameConfig.xlsx";
        [SerializeField] private string variableSheetName = "变量表";
        [SerializeField] private bool loadExcelOnStart;

        private IDisposable _supportRateListener;
        private IDisposable _deterrenceListener;
        private IDisposable _heartRateListener;
        private IDisposable _allDataListener;
        private IDisposable _buttonEventListener;
        private IDisposable _giftEventListener;

        private void Awake()
        {
            if (loadExcelOnStart)
            {
                LoadVariableConfigFromExcel();
            }
            else
            {
                RegisterDefaultValuesByCode();
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

            // 监听所有数值变化，适合调试。
            _allDataListener = GameDataCenter.ObserveAll(OnAnyDataChanged);

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
            _allDataListener?.Dispose();
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
        /// 从 Excel 读取变量表，并注册默认值。
        ///
        /// 文件位置建议：
        /// Assets/StreamingAssets/GameConfig.xlsx
        /// </summary>
        public void LoadVariableConfigFromExcel()
        {
            var path = System.IO.Path.Combine(Application.streamingAssetsPath, excelFileName);
            var rows = ExcelTableLoader.LoadSheet<VariableConfigRow>(path, variableSheetName);

            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.ID))
                {
                    continue;
                }

                GameDataCenter.RegisterDefault(row.ID, row.InitValue);
                Debug.Log($"读取变量配置：{row.ID} {row.Name} 初始值={row.InitValue}");
            }
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

            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                RestartGame();
            }
        }

        private void OnSupportRateChanged(GameDataChangedEvent change)
        {
            SetText(supportRateText, $"支持率：{change.NewValue:0}");
            SetSlider(supportRateSlider, change.NewValue, 0f, 100f);
        }

        private void OnDeterrenceChanged(GameDataChangedEvent change)
        {
            SetText(deterrenceText, $"威慑值：{change.NewValue:0}");
            SetSlider(deterrenceSlider, change.NewValue, 0f, 100f);
        }

        private void OnHeartRateChanged(GameDataChangedEvent change)
        {
            SetText(heartRateText, $"心率：{change.NewValue:0}");
            SetSlider(heartRateSlider, change.NewValue, 40f, 140f);
        }

        private void OnAnyDataChanged(GameDataChangedEvent change)
        {
            Debug.Log($"[数值变化] {change.Key}: {change.OldValue} -> {change.NewValue}, 来源={change.Source}");
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

    /// <summary>
    /// 变量表的一行。
    ///
    /// Excel 第一行表头必须对应这些字段名：
    /// ID | Name | InitValue | MinValue | MaxValue | Desc
    ///
    /// 表格例子：
    /// A_1 | 支持率 | 50 | 0 | 100 | 玩家支持率
    /// D_1 | 威慑值 | 0  | 0 | 100 | 胜利进度
    /// H_1 | 心率   | 70 | 40 | 140 | 当前心率
    /// </summary>
    [TableRow]
    public sealed class VariableConfigRow
    {
        public string ID;
        public string Name;
        public float InitValue;
        public float MinValue;
        public float MaxValue;
        public string Desc;
    }

    /// <summary>
    /// 效果表的一行，先作为示例。
    ///
    /// Excel 第一行表头：
    /// EffectID | TargetID | Op | Value | Desc
    ///
    /// 表格例子：
    /// E_1 | A_1 | Add | 5  | 支持率增加
    /// E_2 | H_1 | Add | -2 | 心率下降
    ///
    /// 以后你们要做“礼物/推特/事件改数值”，就可以从这张表读。
    /// </summary>
    [TableRow]
    public sealed class EffectConfigRow
    {
        public string EffectID;
        public string TargetID;
        public string Op;
        public float Value;
        public string Desc;
    }
}
