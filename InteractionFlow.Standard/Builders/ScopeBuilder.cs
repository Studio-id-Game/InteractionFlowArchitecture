using InteractionFlow.Core.Builders;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace InteractionFlow.Standard.Builders
{
    /// <summary>
    /// 登録済みサービスから <see cref="ScopeHandler"/> を生成する標準ビルダーです。
    /// </summary>
    public class ScopeBuilder : ScopeServices, IScopeBuilder
    {
        /// <summary>
        /// 現在のサービス登録からスコープを生成し、親スコープを関連付けます。
        /// </summary>
        /// <param name="parents">このスコープで解決できないサービスを探索する親スコープ。</param>
        /// <returns>生成されたスコープを管理するハンドラ。</returns>
        public ScopeHandler BuildScope(params ScopeHandler[] parents)
        {
            var services = Services ?? throw new InvalidOperationException();
            try
            {
                var rootProvider = services.BuildServiceProvider();
                var scope = rootProvider.CreateScope();
                var scopedProvider = scope.ServiceProvider;
                return new ScopeHandler(scope, scopedProvider, parents);
            }
            finally
            {
                Services = null;
            }
        }
    }
}
