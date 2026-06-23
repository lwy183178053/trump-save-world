using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Runtime
{
    /// <summary>
    /// 用法很直接：
    /// - Publish：发事件
    /// - Observe：听某一个事件
    /// - ObserveAll：听全部事件
    ///
    /// 适合按钮点击、礼物到达、提案通过、游戏开始/失败这种东西。
    /// </summary>
    public static class GameEventBus
    {
        private static readonly Dictionary<string, List<Action<GameEvent>>> Listeners = new Dictionary<string, List<Action<GameEvent>>>();

        /// <summary>
        /// 所有事件的总广播。
        /// </summary>
        public static event Action<GameEvent> EventRaised;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Listeners.Clear();
            EventRaised = null;
        }

        /// <summary>
        /// 发一个事件。
        /// </summary>
        public static void Publish(string eventName, object payload = null, GameChangeSource source = GameChangeSource.Unknown)
        {
            var gameEvent = new GameEvent(eventName, payload, source, DateTime.UtcNow);

            EventRaised?.Invoke(gameEvent);

            if (!Listeners.TryGetValue(eventName, out var eventListeners))
            {
                return;
            }

            var snapshot = eventListeners.ToArray();
            for (var i = 0; i < snapshot.Length; i++)
            {
                snapshot[i]?.Invoke(gameEvent);
            }
        }

        /// <summary>
        /// 订阅某一个事件名。
        /// </summary>
        public static IDisposable Observe(string eventName, Action<GameEvent> listener)
        {
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            if (!Listeners.TryGetValue(eventName, out var eventListeners))
            {
                eventListeners = new List<Action<GameEvent>>();
                Listeners[eventName] = eventListeners;
            }

            eventListeners.Add(listener);
            return new DisposableAction(() => RemoveListener(eventName, listener));
        }

        /// <summary>
        /// 订阅所有事件。
        /// </summary>
        public static IDisposable ObserveAll(Action<GameEvent> listener)
        {
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            EventRaised += listener;
            return new DisposableAction(() => EventRaised -= listener);
        }

        private static void RemoveListener(string eventName, Action<GameEvent> listener)
        {
            if (!Listeners.TryGetValue(eventName, out var eventListeners))
            {
                return;
            }

            eventListeners.Remove(listener);
            if (eventListeners.Count == 0)
            {
                Listeners.Remove(eventName);
            }
        }
    }
}
