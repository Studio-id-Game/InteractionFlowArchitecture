using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ProgramFlows;

namespace InteractionFlow.Core.Builders
{
    /// <summary>
    /// 登録済みサービスから ProgramFlow を生成するビルダーを表します。
    /// </summary>
    /// <typeparam name="TContext">生成する ProgramFlow が扱うコンテキストの型。</typeparam>
    public interface IProgramFlowBuilder<TContext> : IScopeServices
        where TContext : IFlowContext
    {
        /// <summary>
        /// 現在のサービス登録と追加パラメーターを使用して ProgramFlow を生成します。
        /// </summary>
        /// <typeparam name="TProgramFlow">生成する ProgramFlow の型。</typeparam>
        /// <param name="parameters">ProgramFlow の生成時に DI へ追加で渡すコンストラクタ引数。</param>
        /// <param name="parents">ProgramFlow 用スコープで解決できないサービスを探索する親スコープ。</param>
        /// <returns>生成された ProgramFlow とスコープを管理するハンドラ。</returns>
        ProgramFlowHandler<TContext> BuildProgramFlow<TProgramFlow>(object[] parameters, params ScopeHandler[] parents)
            where TProgramFlow : IProgramFlow<TContext>;

        /// <summary>
        /// 現在のサービス登録を使用して ProgramFlow を生成します。
        /// </summary>
        /// <typeparam name="TProgramFlow">生成する ProgramFlow の型。</typeparam>
        /// <param name="parents">ProgramFlow 用スコープで解決できないサービスを探索する親スコープ。</param>
        /// <returns>生成された ProgramFlow とスコープを管理するハンドラ。</returns>
        ProgramFlowHandler<TContext> BuildProgramFlow<TProgramFlow>(params ScopeHandler[] parents)
            where TProgramFlow : IProgramFlow<TContext>;
    }
}
