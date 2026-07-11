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
        public abstract Task<FlowEndToken> ExecuteAsync(TContext context);

        /// <summary>
        /// 指定された終了結果の最終コンテキストを、ProgramFlow に渡されたコンテキストへ揃えます。
        /// </summary>
        /// <param name="context">ProgramFlow に渡されたコンテキスト。</param>
        /// <param name="end">正規化する終了結果。</param>
        /// <returns>最終コンテキストを揃えた終了結果。</returns>
        protected FlowEndToken NormalizeLastContext(TContext context, FlowEndToken end)
        {
            return end.NormalizeLastContext(context);
        }
    }
}
