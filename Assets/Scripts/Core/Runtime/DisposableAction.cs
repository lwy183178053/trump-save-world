using System;

namespace Core.Runtime
{
    /// <summary>
    /// 一个很小的可释放对象。
    /// 用途：把“取消订阅”这件事包装成一个 IDisposable，
    /// 这样你可以在 OnDisable / OnDestroy 里统一 Dispose。
    /// </summary>
    internal sealed class DisposableAction : IDisposable
    {
        private Action _dispose;

        /// <summary>
        /// 传入一个释放时要执行的方法。
        /// </summary>
        public DisposableAction(Action dispose)
        {
            _dispose = dispose;
        }

        /// <summary>
        /// 只执行一次释放逻辑。
        /// 多次调用不会重复执行。
        /// </summary>
        public void Dispose()
        {
            var dispose = _dispose;
            if (dispose == null)
            {
                return;
            }

            _dispose = null;
            dispose.Invoke();
        }
    }
}
