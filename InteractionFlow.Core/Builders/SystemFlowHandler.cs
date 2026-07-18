using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.SystemFlows;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Core.Builders
{
    /// <summary>
    /// 生成済みの SystemFlow と、その SystemFlow のために作成されたスコープのライフタイムを管理します。
    /// </summary>
    /// <remarks>
    /// SystemFlow の実行対象と依存解決スコープを一体で保持し、破棄時にスコープも解放します。
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
        /// SystemFlow 用に保持しているスコープを破棄し、以降の実行を無効にします。
        /// </summary>
        public void Dispose()
        {
            scope?.Dispose();
            scope = null;
            systemFlow = null;
        }
    }
}
