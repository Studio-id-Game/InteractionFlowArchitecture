using InteractionFlow.Core.Builders;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.SystemFlows;

namespace InteractionFlow.Standard.Builders
{
    /// <summary>
    /// 既存スコープを親として SystemFlow を生成する拡張メソッドを提供します。
    /// </summary>
    public static class BuildSystemFlowExtensions
    {
        /// <summary>
        /// 指定されたスコープを親として SystemFlow を生成します。
        /// </summary>
        /// <typeparam name="TSystemFlow">生成する SystemFlow の型。</typeparam>
        /// <typeparam name="TContext">SystemFlow が扱うコンテキストの型。</typeparam>
        /// <param name="parent">親として使用するスコープ。</param>
        /// <returns>生成された SystemFlow とスコープを管理するハンドラ。</returns>
        public static SystemFlowHandler<TContext> BuildSystemFlow<TSystemFlow, TContext>(this ScopeHandler parent)
            where TSystemFlow : ISystemFlow<TContext>
            where TContext : IFlowContext
        {
            var builder = new SystemFlowBuilder<TContext>();
            return builder.BuildSystemFlow<TSystemFlow>(parent);
        }
    }
}
