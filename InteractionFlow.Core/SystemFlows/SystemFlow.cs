using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Core.SystemFlows
{
    /// <summary>
    /// <see cref="IFlowContext"/> を扱う <see cref="ISystemFlow"/> のデフォルト実装基底クラスです。
    /// </summary>
    /// <param name="dependency">この SystemFlow が依存するフローノード。</param>
    public abstract class SystemFlow(params IDependencyNode[] dependency) : SystemFlow<IFlowContext>(dependency)
    {
    }

    /// <summary>
    /// <see cref="ISystemFlow{TContext}"/> のデフォルト実装基底クラスです。
    /// </summary>
    /// <typeparam name="TContext">SystemFlow が扱うコンテキストの型。</typeparam>
    /// <param name="dependency">この SystemFlow が依存するフローノード。</param>
    public abstract class SystemFlow<TContext>(params IDependencyNode[] dependency) : ISystemFlow<TContext>
        where TContext : IFlowContext
    {
        /// <summary>
        /// この SystemFlow が依存するフローノードを取得します。
        /// </summary>
        public ReadOnlySpan<IDependencyNode> Dependency => dependency;

        /// <summary>
        /// 指定されたコンテキストで SystemFlow を実行します。
        /// </summary>
        /// <param name="context">SystemFlow に渡すコンテキスト。</param>
        /// <returns>SystemFlow の終了結果。</returns>
        public async Task<FlowEndToken> ExecuteAsync(TContext context)
        {
            var end = await ExecuteCoreAsync(context).ConfigureAwait(false);
            return GetEnd(context, end);
        }

        /// <summary>
        /// 指定されたコンテキストで SystemFlow の本体を実行します。
        /// </summary>
        /// <param name="context">SystemFlow に渡すコンテキスト。</param>
        /// <returns>SystemFlow 内の Interaction が返した終了トークン。</returns>
        protected abstract Task<FlowEndToken> ExecuteCoreAsync(TContext context);

        /// <summary>
        /// Interaction の終了トークンを、SystemFlow に渡されたコンテキストへ結合し直します。
        /// </summary>
        /// <param name="context">SystemFlow に渡されたフローコンテキスト。</param>
        /// <param name="interactionEnd">SystemFlow 内の Interaction が返した終了トークン。</param>
        /// <returns>SystemFlow の終了トークン。</returns>
        protected static FlowEndToken GetEnd(IFlowContext context, FlowEndToken interactionEnd)
        {
            return ISystemFlow.GetEnd(context, interactionEnd);
        }
    }
}
