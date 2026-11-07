using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals;
using InteractionFlow.Standard.ConsoleSystem.ExternalPorts;
using System;

namespace InteractionFlow.Standard.ConsoleSystem.Externals
{
    /// <summary>Ctrl+C の購読を参照カウントで共有し、リセット・破棄時にも解放します。</summary>
    public sealed class ConsoleCancellationBinding : SilentExternal, IConsoleCancellationBindingPort, IDisposable
    {
        private sealed class BindScope(ConsoleCancellationBinding binding, object identity) : IDisposable
        {
            private ConsoleCancellationBinding? binding = binding;

            public void Dispose()
            {
                lock (gate)
                {
                    if (binding is null) return;
                    binding.ReleaseReference(identity);
                    // 解除に失敗した場合は参照を残し、Dispose を再試行できるようにする。
                    binding = null;
                }
            }
        }

        private static readonly object gate = new();
        private static ConsoleCancellationBinding? owner;
        private readonly ConsoleCancelEventHandler handler;
        private CancellationObject? target;
        private object? identity;
        private int referenceCount;
        private bool disposed;

        /// <summary>生成時にはイベントを購読しません。</summary>
        public ConsoleCancellationBinding() => handler = OnCancel;

        /// <inheritdoc/>
        public IDisposable Bind(IFlowContext context)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));
            var cancellation = context.Cancellation;
            lock (gate)
            {
                if (disposed) throw new ObjectDisposedException(nameof(ConsoleCancellationBinding));
                if (ReferenceEquals(target, cancellation))
                {
                    var scope = new BindScope(this, identity!);
                    referenceCount = checked(referenceCount + 1);
                    return scope;
                }
                if (owner != null)
                    throw new InvalidOperationException("A console cancellation target is already bound.");

                var newIdentity = new object();
                var newScope = new BindScope(this, newIdentity);
                // 登録が成功してから接続を公開する。通知は同じ gate で同期する。
                Console.CancelKeyPress += handler;
                target = cancellation;
                identity = newIdentity;
                referenceCount = 1;
                owner = this;
                return newScope;
            }
        }

        // gate 内で呼ぶ。リセット前の古いスコープは現在の接続へ影響させない。
        private void ReleaseReference(object scopeIdentity)
        {
            if (!ReferenceEquals(identity, scopeIdentity)) return;
            if (referenceCount > 1) referenceCount--;
            else Release();
        }

        private void Release()
        {
            if (!ReferenceEquals(owner, this)) return;
            // 解除が失敗した場合は所有とカウントを保ち、再試行できるようにする。
            Console.CancelKeyPress -= handler;
            target = null;
            identity = null;
            referenceCount = 0;
            owner = null;
        }

        private void OnCancel(object? sender, ConsoleCancelEventArgs args)
        {
            lock (gate)
            {
                if (target is null) return;
                args.Cancel = true;
                // Monitor は再入可能。同期キャンセルコールバックからのスコープ破棄も許す。
                target.Cancel();
            }
        }

        /// <inheritdoc/>
        public override void ForceResetMemoryState()
        {
            lock (gate) { Release(); }
        }

        /// <summary>全参照を失効させて購読を解除します。DI Scope が本体の寿命を管理します。</summary>
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
