namespace InteractionFlow.Core.Builders
{
    /// <summary>
    /// 登録済みサービスから SystemFlow 用の依存解決スコープを生成するビルダーを表します。
    /// </summary>
    /// <remarks>
    /// このインターフェースは、スコープを生成し、そのライフタイムを <see cref="ScopeHandler"/> に委譲する Core 側の契約です。
    /// 具体的な DI コンテナやサービス登録 API は実装側の責務です。
    /// </remarks>
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
