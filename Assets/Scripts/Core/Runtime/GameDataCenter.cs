using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Runtime
{
    /// <summary>
    ///
    /// - 支持率、威慑值、心率都放这里
    /// - 外部不要直接改字典
    /// - 统一走 Get / Set / Add
    /// - 一旦数值变化，就自动通知监听者
    /// </summary>
    public static class GameDataCenter
    {
        private static readonly Dictionary<string, float> Values = new Dictionary<string, float>();
        private static readonly Dictionary<string, float> Defaults = new Dictionary<string, float>();
        private static readonly Dictionary<string, List<Action<GameDataChangedEvent>>> KeyListeners = new Dictionary<string, List<Action<GameDataChangedEvent>>>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Values.Clear();
            Defaults.Clear();
            KeyListeners.Clear();
        }

        /// <summary>
        /// 注册一个默认值。
        /// 常用于开局初始化。
        /// </summary>
        public static void RegisterDefault(string key, float defaultValue)
        {
            Defaults[key] = defaultValue;
            if (!Values.ContainsKey(key))
            {
                Values[key] = defaultValue;
            }
        }

        /// <summary>
        /// 读取某个值。
        /// 如果没注册，就返回 fallback。
        /// </summary>
        public static float Get(string key, float fallback = 0f)
        {
            if (Values.TryGetValue(key, out var value))
            {
                return value;
            }

            if (Defaults.TryGetValue(key, out value))
            {
                return value;
            }

            return fallback;
        }

        /// <summary>
        /// 设置某个值。
        /// 如果值没变，就不会广播。
        /// </summary>
        public static void Set(string key, float value, GameChangeSource source = GameChangeSource.Unknown)
        {
            var oldValue = Get(key);
            if (Mathf.Approximately(oldValue, value))
            {
                return;
            }

            Values[key] = value;
            Emit(new GameDataChangedEvent(key, oldValue, value, source, false, DateTime.UtcNow));
        }

        /// <summary>
        /// 在当前值基础上加一个增量。
        /// </summary>
        public static void Add(string key, float delta, GameChangeSource source = GameChangeSource.Unknown)
        {
            Set(key, Get(key) + delta, source);
        }

        /// <summary>
        /// 把当前值限制在某个范围里。
        /// </summary>
        public static void Clamp(string key, float minValue, float maxValue, GameChangeSource source = GameChangeSource.Unknown)
        {
            Set(key, Mathf.Clamp(Get(key), minValue, maxValue), source);
        }

        /// <summary>
        /// 重置单个值到默认值。
        /// </summary>
        public static void Reset(string key, GameChangeSource source = GameChangeSource.Restore)
        {
            if (!Defaults.TryGetValue(key, out var defaultValue))
            {
                return;
            }

            Set(key, defaultValue, source);
        }

        /// <summary>
        /// 重置所有已注册默认值。
        /// </summary>
        public static void ResetAll(GameChangeSource source = GameChangeSource.Restore)
        {
            foreach (var pair in Defaults)
            {
                Set(pair.Key, pair.Value, source);
            }
        }

        /// <summary>
        /// 订阅某一个 key。
        /// replayCurrentValue=true 时，会先把当前值推一次。
        /// 这样 UI 晚绑定也不会空。
        /// </summary>
        public static IDisposable Observe(string key, Action<GameDataChangedEvent> listener, bool replayCurrentValue = true)
        {
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            if (!KeyListeners.TryGetValue(key, out var listeners))
            {
                listeners = new List<Action<GameDataChangedEvent>>();
                KeyListeners[key] = listeners;
            }

            listeners.Add(listener);

            if (replayCurrentValue)
            {
                var current = Get(key);
                listener(new GameDataChangedEvent(key, current, current, GameChangeSource.Replay, true, DateTime.UtcNow));
            }

            return new DisposableAction(() => RemoveListener(key, listener));
        }

        private static void Emit(GameDataChangedEvent change)
        {
            if (!KeyListeners.TryGetValue(change.Key, out var listeners))
            {
                return;
            }

            var snapshot = listeners.ToArray();
            for (var i = 0; i < snapshot.Length; i++)
            {
                snapshot[i]?.Invoke(change);
            }
        }

        private static void RemoveListener(string key, Action<GameDataChangedEvent> listener)
        {
            if (!KeyListeners.TryGetValue(key, out var listeners))
            {
                return;
            }

            listeners.Remove(listener);
            if (listeners.Count == 0)
            {
                KeyListeners.Remove(key);
            }
        }
    }
}
