namespace InteractionFlow.Core.Builders
{
    /// <summary>
    /// 複数のサービス登録をまとめて適用するプロファイルを表します。
    /// </summary>
    public interface IScopeProfile
    {
        /// <summary>
        /// 指定されたサービス構成に、このプロファイルのサービス登録を追加します。
        /// </summary>
        /// <param name="builder">登録を追加するサービス構成。</param>
        void Configure(IScopeServices builder);
    }
}
