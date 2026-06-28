using System;
using UnityEngine;

namespace CWGame
{
    /// <summary>
    /// 提示事件系统 - 其他系统广播，TipsUI 监听
    /// </summary>
    public static class TipEvents
    {
        // 提示请求（其他系统广播，TipsUI 监听）
        public static event Action<TipSO> OnTipRequested;
        public static void RequestTip(TipSO tipSO) => OnTipRequested?.Invoke(tipSO);

        // 提示开始（TipsUI 广播，其他系统监听）
        public static event Action<TipNode> OnTipStart;
        public static void TriggerTipStart(TipNode node) => OnTipStart?.Invoke(node);

        // 提示节点更新（TipsUI 广播，其他系统监听）
        public static event Action<int, TipNode> OnTipNodeUpdate;
        public static void TriggerTipNodeUpdate(int index, TipNode node) => OnTipNodeUpdate?.Invoke(index, node);

        // 提示结束（TipsUI 广播，其他系统监听）
        public static event Action OnTipEnd;
        public static void TriggerTipEnd() => OnTipEnd?.Invoke();

        // 选项选中
        public static event Action<int> OnChoiceSelected;
        public static void TriggerChoiceSelected(int index) => OnChoiceSelected?.Invoke(index);
    }
}
