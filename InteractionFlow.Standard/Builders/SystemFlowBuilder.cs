using InteractionFlow.Core.Builders;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.SystemFlows;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace InteractionFlow.Standard.Builders
{
    /// <summary>
    /// 登録済みサービスから SystemFlow と専用スコープを生成する標準ビルダーです。
    /// </summary>
    /// <typeparam name="TContext">生成する SystemFlow が扱うコンテキストの型。</typeparam>
    public class SystemFlowBuilder<TContext> : ScopeServices, ISystemFlowBuilder<TContext>
        where TContext : IFlowContext
    {
        private ScopeHandler BuildScope(params ScopeHandler[] parents)
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

        /// <summary>
        /// 現在のサービス登録を使用して SystemFlow を生成します。
        /// </summary>
        /// <typeparam name="TSystemFlow">生成する SystemFlow の型。</typeparam>
        /// <param name="parents">SystemFlow 用スコープで解決できないサービスを探索する親スコープ。</param>
        /// <returns>生成された SystemFlow とスコープを管理するハンドラ。</returns>
        public SystemFlowHandler<TContext> BuildSystemFlow<TSystemFlow>(params ScopeHandler[] parents)
            where TSystemFlow : ISystemFlow<TContext>
        {
            var scope = BuildScope(parents);
            var systemFlow = ActivatorUtilities.CreateInstance<TSystemFlow>(scope)
                ?? throw new InvalidOperationException();

            return new SystemFlowHandler<TContext>(scope, systemFlow);
        }

        /// <summary>
        /// 現在のサービス登録と追加パラメーターを使用して SystemFlow を生成します。
        /// </summary>
        /// <typeparam name="TSystemFlow">生成する SystemFlow の型。</typeparam>
        /// <param name="parameters">SystemFlow の生成時に DI へ追加で渡すコンストラクタ引数。</param>
        /// <param name="parents">SystemFlow 用スコープで解決できないサービスを探索する親スコープ。</param>
        /// <returns>生成された SystemFlow とスコープを管理するハンドラ。</returns>
        public SystemFlowHandler<TContext> BuildSystemFlow<TSystemFlow>(object[] parameters, params ScopeHandler[] parents)
            where TSystemFlow : ISystemFlow<TContext>
        {
            var scope = BuildScope(parents);
            var systemFlow = ActivatorUtilities.CreateInstance<TSystemFlow>(scope, parameters)
                ?? throw new InvalidOperationException();

            return new SystemFlowHandler<TContext>(scope, systemFlow);
        }
    }
}
