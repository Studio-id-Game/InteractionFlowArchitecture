namespace InteractionFlow.Core.Builders
{
    /// <summary>
    /// 登録済みサービスから DI スコープを生成するビルダーを表します。
    /// </summary>
    public interface IScopeBuilder : IScopeServices
    {
        /// <summary>
        /// 現在のサービス登録からスコープを生成し、必要に応じて親スコープを関連付けます。
        /// </summary>
        /// <param name="parents">このスコープで解決できないサービスを探索する親スコープ。</param>
        /// <returns>生成されたスコープを管理するハンドラ。</returns>
        ScopeHandler BuildScope(params ScopeHandler[] parents);
    }
}
