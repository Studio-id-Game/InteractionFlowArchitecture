using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.SystemFlows;

namespace InteractionFlow.Core.Builders
{
    /// <summary>
    /// 登録済みサービスから SystemFlow とその実行スコープを生成するビルダーを表します。
    /// </summary>
    /// <remarks>
    /// SystemFlowBuilder は、InteractionFlow Architecture における SystemFlow 構築の入口です。
    /// Core では、SystemFlow の生成、依存解決スコープ、親スコープ探索の契約だけを定義します。
    /// Microsoft.Extensions.DependencyInjection などの具体的な DI 実装は Standard 側などで提供されます。
    /// </remarks>
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
