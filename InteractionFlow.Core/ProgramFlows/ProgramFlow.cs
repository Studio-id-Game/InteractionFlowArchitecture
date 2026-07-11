using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Core.ProgramFlows
{
    /// <summary>
    /// <see cref="IFlowContext"/> を扱う <see cref="IProgramFlow"/> のデフォルト実装基底クラスです。
    /// </summary>
    /// <param name="dependency">この ProgramFlow が依存するフローノード。</param>
    public abstract class ProgramFlow(params IFlowNode[] dependency) : ProgramFlow<IFlowContext>(dependency)
    {
    }

    /// <summary>
    /// <see cref="IProgramFlow{TContext}"/> のデフォルト実装基底クラスです。
    /// </summary>
    /// <typeparam name="TContext">ProgramFlow が扱うコンテキストの型。</typeparam>
    /// <param name="dependency">この ProgramFlow が依存するフローノード。</param>
    public abstract class ProgramFlow<TContext>(params IFlowNode[] dependency) : IProgramFlow<TContext>
        where TContext : IFlowContext
    {
        /// <summary>
        /// この ProgramFlow が依存するフローノードを取得します。
        /// </summary>
        public ReadOnlySpan<IFlowNode> Dependency => dependency;

        /// <summary>
        /// 指定されたコンテキストで ProgramFlow を実行します。
        /// </summary>
        /// <param name="context">ProgramFlow に渡すコンテキスト。</param>
        /// <returns>ProgramFlow の終了結果。</returns>
        public async Task<FlowEndToken> ExecuteAsync(TContext context)
        {
            var end = await ExecuteCoreAsync(context).ConfigureAwait(false);
            return GetEnd(context, end);
        }

        /// <summary>
        /// 指定されたコンテキストで ProgramFlow の本体を実行します。
        /// </summary>
        /// <param name="context">ProgramFlow に渡すコンテキスト。</param>
        /// <returns>ProgramFlow 内の Interaction が返した終了トークン。</returns>
        protected abstract Task<FlowEndToken> ExecuteCoreAsync(TContext context);

        /// <summary>
        /// Interaction の終了トークンを、ProgramFlow に渡されたコンテキストへ結合し直します。
        /// </summary>
        /// <param name="context">ProgramFlow に渡されたフローコンテキスト。</param>
        /// <param name="interactionEnd">ProgramFlow 内の Interaction が返した終了トークン。</param>
        /// <returns>ProgramFlow の終了トークン。</returns>
        protected static FlowEndToken GetEnd(IFlowContext context, FlowEndToken interactionEnd)
        {
            return IProgramFlow.GetEnd(context, interactionEnd);
        }
    }
}
