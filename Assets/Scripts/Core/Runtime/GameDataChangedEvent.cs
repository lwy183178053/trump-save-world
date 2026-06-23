using System;

namespace Core.Runtime
{
    /// <summary>
    /// 数值变化事件。
    /// 谁改了、改了多少、什么时候改的，都放在这里。
    /// UI 或其他系统订阅这个事件后，就能被动刷新显示。
    /// </summary>
    public readonly struct GameDataChangedEvent
    {
        /// <summary>被修改的键名，例如 A_1 / D_1 / H_1。</summary>
        public string Key { get; }
        /// <summary>修改前的值。</summary>
        public float OldValue { get; }
        /// <summary>修改后的值。</summary>
        public float NewValue { get; }
        /// <summary>变化量，等于 NewValue - OldValue。</summary>
        public float Delta => NewValue - OldValue;
        /// <summary>这次变化来自哪里。</summary>
        public GameChangeSource Source { get; }
        /// <summary>UTC 时间戳，方便调试和记录日志。</summary>
        public DateTime TimestampUtc { get; }
        /// <summary>是否为“回放当前值”，不是实际的新变化。</summary>
        public bool IsReplay { get; }

        /// <summary>
        /// 构造一个数据变化事件。
        /// </summary>
        public GameDataChangedEvent(
            string key,
            float oldValue,
            float newValue,
            GameChangeSource source,
            bool isReplay,
            DateTime timestampUtc)
        {
            Key = key;
            OldValue = oldValue;
            NewValue = newValue;
            Source = source;
            IsReplay = isReplay;
            TimestampUtc = timestampUtc;
        }
    }
}
