using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.SystemFlows;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Core.Builders
{
    /// <summary>
    /// SystemFlow を実行対象として保持し、その実行のために作成された専用
    /// <see cref="ScopeHandler"/> のライフタイムを管理します。
    /// </summary>
    /// <remarks>
    /// 破棄時は専用 ScopeHandler を破棄して以後の実行を無効にします。
    /// SystemFlow 自体、探索先の親 ScopeHandler、実行時に渡された <see cref="IFlowContext"/> の
    /// 所有権は取得せず、これらを直接破棄しません。
    /// </remarks>
    /// <typeparam name="TContext">SystemFlow が扱うコンテキストの型。</typeparam>
    /// <param name="scope">SystemFlow の依存解決に使用されたスコープ。</param>
    /// <param name="systemFlow">実行対象の SystemFlow。</param>
    public sealed class SystemFlowHandler<TContext>(ScopeHandler scope, ISystemFlow<TContext> systemFlow) : IDisposable where TContext : IFlowContext
    {
        private ScopeHandler? scope = scope;
        private ISystemFlow<TContext>? systemFlow = systemFlow;

        /// <summary>
        /// 保持している SystemFlow を IDependencyNode として取得します
        /// </summary>
        public IDependencyNode Root => systemFlow ?? throw new ObjectDisposedException(nameof(SystemFlowHandler<TContext>));

        /// <summary>
        /// 保持している SystemFlow を指定されたコンテキストで実行します。
        /// </summary>
        /// <param name="context">SystemFlow に渡すコンテキスト。</param>
        /// <returns>SystemFlow の終了結果。</returns>
        /// <exception cref="ObjectDisposedException">このハンドラが破棄済みの場合に発生します。</exception>
        public async Task<FlowEndToken> ExecuteAsync(TContext context)
        {
            var systemFlow = this.systemFlow ?? throw new ObjectDisposedException(nameof(SystemFlowHandler<TContext>));
            return await systemFlow.ExecuteAsync(context).ConfigureAwait(false);
        }

        /// <summary>
        /// SystemFlow 用に保持している専用スコープを破棄し、以降の実行を無効にします。
        /// </summary>
        /// <remarks>
        /// SystemFlow 自体、探索先の親スコープ、実行時に渡されたコンテキストは直接破棄しません。
        /// </remarks>
        public void Dispose()
        {
            var scope = this.scope;

            this.scope = null;
            this.systemFlow = null;

            scope?.Dispose();
        }
    }
}
