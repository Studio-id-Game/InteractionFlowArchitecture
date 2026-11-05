using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.ConsoleSystem.Externals
{
    /// <summary>Console 入力の占有と解放を共有する実装基底です。</summary>
    public abstract class ConsoleInputBase : Operation
    {
        // 行・キー・DI Scope の違いによらず、プロセス内で一件だけ入力を許可する。
        private static int inputInUse;

        /// <summary>派生実装の依存ノードを保持します。</summary>
        protected ConsoleInputBase(params IDependencyNode[] dependency) : base(dependency)
        {
        }

        /// <summary>入力を占有し、処理の成功・失敗・キャンセルによらず解放します。</summary>
        protected static async ValueTask<T> ReadAsync<T>(
            IFlowContext context, Func<CancellationToken, ValueTask<T>> read)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));
            var token = context.Cancellation.GetToken();
            token.ThrowIfCancellationRequested();

            if (Interlocked.CompareExchange(ref inputInUse, 1, 0) != 0)
                throw new InvalidOperationException("Another console input is already in progress.");

            try
            {
                return await read(token).ConfigureAwait(false);
            }
            finally
            {
                Volatile.Write(ref inputInUse, 0);
            }
        }

        /// <summary>入力を中断せず、OS 側の入力状態も変更しません。</summary>
        public override void ForceResetMemoryState()
        {
        }
    }
}
