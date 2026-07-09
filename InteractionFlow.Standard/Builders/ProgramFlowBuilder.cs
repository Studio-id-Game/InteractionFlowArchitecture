using InteractionFlow.Core.Builders;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ProgramFlows;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace InteractionFlow.Standard.Builders
{
    /// <summary>
    /// 登録済みサービスから ProgramFlow と専用スコープを生成する標準ビルダーです。
    /// </summary>
    /// <typeparam name="TContext">生成する ProgramFlow が扱うコンテキストの型。</typeparam>
    public class ProgramFlowBuilder<TContext> : ScopeServices, IProgramFlowBuilder<TContext>
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
        /// 現在のサービス登録を使用して ProgramFlow を生成します。
        /// </summary>
        /// <typeparam name="TProgramFlow">生成する ProgramFlow の型。</typeparam>
        /// <param name="parents">ProgramFlow 用スコープで解決できないサービスを探索する親スコープ。</param>
        /// <returns>生成された ProgramFlow とスコープを管理するハンドラ。</returns>
        public ProgramFlowHandler<TContext> BuildProgramFlow<TProgramFlow>(params ScopeHandler[] parents)
            where TProgramFlow : IProgramFlow<TContext>
        {
            var scope = BuildScope(parents);
            var programFlow = ActivatorUtilities.CreateInstance<TProgramFlow>(scope)
                ?? throw new InvalidOperationException();

            return new ProgramFlowHandler<TContext>(scope, programFlow);
        }

        /// <summary>
        /// 現在のサービス登録と追加パラメーターを使用して ProgramFlow を生成します。
        /// </summary>
        /// <typeparam name="TProgramFlow">生成する ProgramFlow の型。</typeparam>
        /// <param name="parameters">ProgramFlow の生成時に DI へ追加で渡すコンストラクタ引数。</param>
        /// <param name="parents">ProgramFlow 用スコープで解決できないサービスを探索する親スコープ。</param>
        /// <returns>生成された ProgramFlow とスコープを管理するハンドラ。</returns>
        public ProgramFlowHandler<TContext> BuildProgramFlow<TProgramFlow>(object[] parameters, params ScopeHandler[] parents)
            where TProgramFlow : IProgramFlow<TContext>
        {
            var scope = BuildScope(parents);
            var programFlow = ActivatorUtilities.CreateInstance<TProgramFlow>(scope, parameters)
                ?? throw new InvalidOperationException();

            return new ProgramFlowHandler<TContext>(scope, programFlow);
        }
    }
}
