using System.Collections.Generic;

namespace Core.Runtime
{
    /// <summary>
    /// 当前一份状态快照。
    /// 它适合做“开局初始值”“重置值”或者“存档值”的容器。
    /// </summary>
    public sealed class GameStateSnapshot
    {
        private readonly Dictionary<string, float> _values;

        /// <summary>
        /// 创建一个空快照。
        /// </summary>
        public GameStateSnapshot()
        {
            _values = new Dictionary<string, float>();
        }

        /// <summary>
        /// 用一个已有字典拷贝出快照。
        /// 注意这里是复制，不会直接引用原字典。
        /// </summary>
        public GameStateSnapshot(IDictionary<string, float> values)
        {
            _values = new Dictionary<string, float>(values);
        }

        /// <summary>
        /// 只读查看所有值。
        /// </summary>
        public IReadOnlyDictionary<string, float> Values => _values;

        /// <summary>
        /// 取某个键的值。
        /// </summary>
        public bool TryGetValue(string key, out float value)
        {
            return _values.TryGetValue(key, out value);
        }

        /// <summary>
        /// 设置某个键的值。
        /// 这个类本身只负责存数据，不负责广播事件。
        /// </summary>
        public void SetValue(string key, float value)
        {
            _values[key] = value;
        }
    }
}
