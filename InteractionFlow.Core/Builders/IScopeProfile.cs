namespace InteractionFlow.Core.Builders
{
    /// <summary>
    /// 複数のサービス登録をまとめて適用するプロファイルを表します。
    /// </summary>
    /// <remarks>
    /// SystemFlowBuilder や ScopeBuilder に対して、用途ごとの依存関係セットを再利用可能な形で適用するための Core 契約です。
    /// </remarks>
    public interface IScopeProfile
    {
        /// <summary>
        /// 指定されたサービス構成に、このプロファイルのサービス登録を追加します。
        /// </summary>
        /// <param name="builder">登録を追加するサービス構成。</param>
        void Configure(IScopeServices builder);
    }
}
