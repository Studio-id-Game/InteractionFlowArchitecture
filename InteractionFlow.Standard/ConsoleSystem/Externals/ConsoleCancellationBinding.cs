using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals;
using InteractionFlow.Standard.ConsoleSystem.ExternalPorts;
using System;

namespace InteractionFlow.Standard.ConsoleSystem.Externals
{
    /// <summary>Ctrl+C の購読を保持し、解除・リセット・破棄時に解放します。</summary>
    public sealed class ConsoleCancellationBinding : SilentExternal, IConsoleCancellationBindingPort, IDisposable
    {
        private static readonly object gate = new();
        private static ConsoleCancellationBinding? owner;
        private readonly ConsoleCancelEventHandler handler;
        private CancellationObject? target;
        private bool disposed;

        /// <summary>標準コンソールに接続します。生成時にはイベントを購読しません。</summary>
        public ConsoleCancellationBinding()
        {
            handler = OnCancel;
        }

        /// <inheritdoc/>
        public void Bind(IFlowContext context)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));
            var cancellation = context.Cancellation;

            lock (gate)
            {
                if (disposed) throw new ObjectDisposedException(nameof(ConsoleCancellationBinding));
                if (ReferenceEquals(target, cancellation)) return;
                if (owner != null)
                    throw new InvalidOperationException("A console cancellation target is already bound.");

                target = cancellation;
                owner = this;
                try
                {
                    Console.CancelKeyPress += handler;
                }
                catch
                {
                    target = null;
                    owner = null;
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public void Unbind()
        {
            lock (gate)
            {
                Release();
            }
        }

        private void Release()
        {
            if (!ReferenceEquals(owner, this)) return;

            // 解除が失敗した場合は所有を保ち、再試行できるようにする。
            Console.CancelKeyPress -= handler;
            target = null;
            owner = null;
        }

        private void OnCancel(object? sender, ConsoleCancelEventArgs args)
        {
            lock (gate)
            {
                if (target is null) return;

                args.Cancel = true;
                // Monitor は再入可能。Cancel の同期コールバックから Unbind されても自己待機しない。
                target.Cancel();
            }
        }

        /// <inheritdoc/>
        public override void ForceResetMemoryState() => Unbind();

        /// <summary>購読を解除します。複数回の呼び出しは安全です。</summary>
        public void Dispose()
        {
            lock (gate)
            {
                Release();
                disposed = true;
            }
        }
    }
}
