using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ProgramFlows;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Core.Builders
{
    /// <summary>
    /// 生成済みの ProgramFlow と、その ProgramFlow のために作成されたスコープのライフタイムを管理します。
    /// </summary>
    /// <typeparam name="TContext">ProgramFlow が扱うコンテキストの型。</typeparam>
    /// <param name="scope">ProgramFlow の依存解決に使用されたスコープ。</param>
    /// <param name="programFlow">実行対象の ProgramFlow。</param>
    public sealed class ProgramFlowHandler<TContext>(ScopeHandler scope, IProgramFlow<TContext> programFlow) : IDisposable where TContext : IFlowContext
    {
        private ScopeHandler? scope = scope;
        private IProgramFlow<TContext>? programFlow = programFlow;

        /// <summary>
        /// 保持している ProgramFlow を指定されたコンテキストで実行します。
        /// </summary>
        /// <param name="context">ProgramFlow に渡すコンテキスト。</param>
        /// <returns>ProgramFlow の終了結果。</returns>
        /// <exception cref="InvalidOperationException">このハンドラが破棄済みの場合に発生します。</exception>
        public async Task<FlowEndToken> ExecuteAsync(TContext context)
        {
            var programFlow = this.programFlow ?? throw new InvalidOperationException();
            return await programFlow.ExecuteAsync(context).ConfigureAwait(false);
        }

        /// <summary>
        /// ProgramFlow 用に保持しているスコープを破棄し、以降の実行を無効にします。
        /// </summary>
        public void Dispose()
        {
            scope?.Dispose();
            scope = null;
            programFlow = null;
        }
    }
}
