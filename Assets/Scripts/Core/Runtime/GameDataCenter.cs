using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Runtime
{
    /// <summary>
    /// 简化版全局数值中心。
    ///
    /// 你可以把它理解成一个“全局字典”：
    /// - 支持率、威慑值、心率都放这里
    /// - 外部不要直接改字典
    /// - 统一走 Get / Set / Add
    /// - 一旦数值变化，就自动通知监听者
    ///
    /// 这个版本是给比赛用的，刻意做得很轻：
    /// 只保留最常用的功能，不做复杂的批处理和存档系统。
    /// </summary>
    public static class GameDataCenter
    {
        private static readonly Dictionary<string, float> Values = new Dictionary<string, float>();
        private static readonly Dictionary<string, float> Defaults = new Dictionary<string, float>();
        private static readonly Dictionary<string, List<Action<GameDataChangedEvent>>> KeyListeners = new Dictionary<string, List<Action<GameDataChangedEvent>>>();

        /// <summary>
        /// 所有数值变化的总事件。
        /// 适合调试面板、日志、全局 UI。
        /// </summary>
        public static event Action<GameDataChangedEvent> ValueChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Values.Clear();
            Defaults.Clear();
            KeyListeners.Clear();
            ValueChanged = null;
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

        /// <summary>
        /// 订阅所有变化。
        /// </summary>
        public static IDisposable ObserveAll(Action<GameDataChangedEvent> listener, bool replayCurrentState = false)
        {
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            ValueChanged += listener;

            if (replayCurrentState)
            {
                foreach (var pair in Values)
                {
                    listener(new GameDataChangedEvent(pair.Key, pair.Value, pair.Value, GameChangeSource.Replay, true, DateTime.UtcNow));
                }
            }

            return new DisposableAction(() => ValueChanged -= listener);
        }

        // 下面这些方法是老名字，保留一份，避免你现有脚本改太多。
        public static float GetValue(string key, float fallback = 0f) => Get(key, fallback);
        public static void SetValue(string key, float value, GameChangeSource source = GameChangeSource.Unknown) => Set(key, value, source);
        public static void AddValue(string key, float delta, GameChangeSource source = GameChangeSource.Unknown) => Add(key, delta, source);
        public static void ClampValue(string key, float minValue, float maxValue, GameChangeSource source = GameChangeSource.Unknown) => Clamp(key, minValue, maxValue, source);
        public static void ResetValue(string key, GameChangeSource source = GameChangeSource.Restore) => Reset(key, source);
        public static IDisposable ObserveValue(string key, Action<GameDataChangedEvent> listener, bool replayCurrentValue = true) => Observe(key, listener, replayCurrentValue);

        private static void Emit(GameDataChangedEvent change)
        {
            ValueChanged?.Invoke(change);

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
