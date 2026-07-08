namespace InteractionFlow.Core.Builders
{
    /// <summary>
    /// スコープ内で利用するサービス登録を構成するためのインターフェースです。
    /// </summary>
    public interface IScopeServices
    {
        /// <summary>
        /// 指定したサービス型に対して実装型をスコープ単位のサービスとして登録します。
        /// </summary>
        /// <typeparam name="TService">登録するサービスの型。</typeparam>
        /// <typeparam name="TImplementation">サービスとして生成する実装型。</typeparam>
        /// <returns>続けて登録を行うための現在のサービス構成。</returns>
        IScopeServices Use<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService;

        /// <summary>
        /// 指定した型をスコープ単位のサービスとして登録します。
        /// </summary>
        /// <typeparam name="TService">登録するサービスの型。</typeparam>
        /// <returns>続けて登録を行うための現在のサービス構成。</returns>
        IScopeServices Use<TService>()
            where TService : class;

        /// <summary>
        /// 指定したサービス型に対して実装型を解決のたびに生成されるサービスとして登録します。
        /// </summary>
        /// <typeparam name="TService">登録するサービスの型。</typeparam>
        /// <typeparam name="TImplementation">サービスとして生成する実装型。</typeparam>
        /// <returns>続けて登録を行うための現在のサービス構成。</returns>
        IScopeServices UseTransient<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService;

        /// <summary>
        /// 指定した型を解決のたびに生成されるサービスとして登録します。
        /// </summary>
        /// <typeparam name="TService">登録するサービスの型。</typeparam>
        /// <returns>続けて登録を行うための現在のサービス構成。</returns>
        IScopeServices UseTransient<TService>()
            where TService : class;

        /// <summary>
        /// プロファイルに定義されたサービス登録をこのサービス構成へ適用します。
        /// </summary>
        /// <param name="profile">適用するサービス登録プロファイル。</param>
        /// <returns>続けて登録を行うための現在のサービス構成。</returns>
        IScopeServices Apply(IScopeProfile profile);
    }
}
