using InteractionFlow.Core.Builders;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ProgramFlows;

namespace InteractionFlow.Standard.Builders
{
    /// <summary>
    /// 既存スコープを親として ProgramFlow を生成する拡張メソッドを提供します。
    /// </summary>
    public static class BuildProgramFlowExtensions
    {
        /// <summary>
        /// 指定されたスコープを親として ProgramFlow を生成します。
        /// </summary>
        /// <typeparam name="TProgramFlow">生成する ProgramFlow の型。</typeparam>
        /// <typeparam name="TContext">ProgramFlow が扱うコンテキストの型。</typeparam>
        /// <param name="parent">親として使用するスコープ。</param>
        /// <returns>生成された ProgramFlow とスコープを管理するハンドラ。</returns>
        public static ProgramFlowHandler<TContext> BuildProgramFlow<TProgramFlow, TContext>(this ScopeHandler parent)
            where TProgramFlow : IProgramFlow<TContext>
            where TContext : IFlowContext
        {
            var builder = new ProgramFlowBuilder<TContext>();
            return builder.BuildProgramFlow<TProgramFlow>(parent);
        }
    }
}
