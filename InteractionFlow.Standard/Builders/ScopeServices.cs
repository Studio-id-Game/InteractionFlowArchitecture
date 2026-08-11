using InteractionFlow.Core.Builders;
using InteractionFlow.Core.Entities.Architectures;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace InteractionFlow.Standard.Builders
{
    /// <summary>
    /// Microsoft.Extensions.DependencyInjection を使用するサービス登録の共通実装です。
    /// </summary>
    public abstract class ScopeServices : IScopeServices
    {
        /// <summary>
        /// 自身を初期化し、<c>params IDependencyNode[]</c> を解決するための既定の空配列を登録します。
        /// </summary>
        protected ScopeServices()
        {
            Services.AddSingleton<IDependencyNode[]>([]);
        }

        /// <summary>
        /// スコープ生成前のサービス登録コレクションを取得または設定します。
        /// </summary>
        protected ServiceCollection? Services { get; set; } = new();

        /// <summary>
        /// 指定した型を scoped サービスとして登録します。
        /// </summary>
        /// <typeparam name="TService">登録するサービスの型。</typeparam>
        /// <returns>続けて登録を行うための現在のサービス構成。</returns>
        public IScopeServices Use<TService>()
            where TService : class
        {
            var services = Services ?? throw new InvalidOperationException();
            services.AddScoped<TService>();
            return this;
        }

        /// <summary>
        /// 指定したサービス型と実装型を scoped サービスとして登録します。
        /// </summary>
        /// <typeparam name="TService">登録するサービスの型。</typeparam>
        /// <typeparam name="TImplementation">サービスとして生成する実装型。</typeparam>
        /// <returns>続けて登録を行うための現在のサービス構成。</returns>
        public IScopeServices Use<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            var services = Services ?? throw new InvalidOperationException();
            services.AddScoped<TService, TImplementation>();
            return this;
        }

        /// <summary>
        /// 指定した型を transient サービスとして登録します。
        /// </summary>
        /// <typeparam name="TService">登録するサービスの型。</typeparam>
        /// <returns>続けて登録を行うための現在のサービス構成。</returns>
        public IScopeServices UseTransient<TService>()
            where TService : class
        {
            var services = Services ?? throw new InvalidOperationException();
            services.AddTransient<TService>();
            return this;
        }

        /// <summary>
        /// 指定したサービス型と実装型を transient サービスとして登録します。
        /// </summary>
        /// <typeparam name="TService">登録するサービスの型。</typeparam>
        /// <typeparam name="TImplementation">サービスとして生成する実装型。</typeparam>
        /// <returns>続けて登録を行うための現在のサービス構成。</returns>
        public IScopeServices UseTransient<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            var services = Services ?? throw new InvalidOperationException();
            services.AddTransient<TService, TImplementation>();
            return this;
        }

        /// <summary>
        /// 指定したプロファイルのサービス登録を適用します。
        /// </summary>
        /// <param name="profile">適用する登録プロファイル。</param>
        /// <returns>続けて登録を行うための現在のサービス構成。</returns>
        public IScopeServices Apply(IScopeProfile profile)
        {
            profile.Configure(this);
            return this;
        }
    }
}
