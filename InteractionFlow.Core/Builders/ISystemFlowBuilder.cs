using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.SystemFlows;

namespace InteractionFlow.Core.Builders
{
    /// <summary>
    /// 登録済みサービスから SystemFlow を生成するビルダーを表します。
    /// </summary>
    /// <typeparam name="TContext">生成する SystemFlow が扱うコンテキストの型。</typeparam>
    public interface ISystemFlowBuilder<TContext> : IScopeServices
        where TContext : IFlowContext
    {
        /// <summary>
        /// 現在のサービス登録と追加パラメーターを使用して SystemFlow を生成します。
        /// </summary>
        /// <typeparam name="TSystemFlow">生成する SystemFlow の型。</typeparam>
        /// <param name="parameters">SystemFlow の生成時に DI へ追加で渡すコンストラクタ引数。</param>
        /// <param name="parents">SystemFlow 用スコープで解決できないサービスを探索する親スコープ。</param>
        /// <returns>生成された SystemFlow とスコープを管理するハンドラ。</returns>
        SystemFlowHandler<TContext> BuildSystemFlow<TSystemFlow>(object[] parameters, params ScopeHandler[] parents)
            where TSystemFlow : ISystemFlow<TContext>;

        /// <summary>
        /// 現在のサービス登録を使用して SystemFlow を生成します。
        /// </summary>
        /// <typeparam name="TSystemFlow">生成する SystemFlow の型。</typeparam>
        /// <param name="parents">SystemFlow 用スコープで解決できないサービスを探索する親スコープ。</param>
        /// <returns>生成された SystemFlow とスコープを管理するハンドラ。</returns>
        SystemFlowHandler<TContext> BuildSystemFlow<TSystemFlow>(params ScopeHandler[] parents)
            where TSystemFlow : ISystemFlow<TContext>;
    }
}
