using System;

namespace Core.Runtime
{
    /// <summary>
    /// 离散事件。
    /// 和数值变化不同，这类事件不一定有“前后值”，
    /// 比如按钮按下、礼物到达、提案通过、游戏胜负。
    /// </summary>
    public readonly struct GameEvent
    {
        /// <summary>事件名，例如 button.pressed / gift.received。</summary>
        public string Name { get; }
        /// <summary>事件附带的数据，可以是 id、对象、参数包等。</summary>
        public object Payload { get; }
        /// <summary>事件来源。</summary>
        public GameChangeSource Source { get; }
        /// <summary>UTC 时间戳。</summary>
        public DateTime TimestampUtc { get; }

        /// <summary>
        /// 构造一个事件。
        /// </summary>
        public GameEvent(string name, object payload, GameChangeSource source, DateTime timestampUtc)
        {
            Name = name;
            Payload = payload;
            Source = source;
            TimestampUtc = timestampUtc;
        }
    }
}
