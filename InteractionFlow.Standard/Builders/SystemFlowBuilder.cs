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
    /// <remarks>
    /// <c>BuildSystemFlow</c> を一度試みるとサービス登録は消費され、
    /// Build の成否にかかわらず、このインスタンスを再利用できません。
    /// </remarks>
    /// <typeparam name="TContext">生成する SystemFlow が扱うコンテキストの型。</typeparam>
    public class SystemFlowBuilder<TContext> : ScopeServices, ISystemFlowBuilder<TContext>
        where TContext : IFlowContext
    {
        private ScopeHandler BuildScope(params ScopeHandler[] parents)
        {
            var services = Services ?? throw new InvalidOperationException();
            try
            {
                return ScopeHandlerFactory.Create(services, parents);
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
        /// <returns>生成された SystemFlow を保持し、専用スコープのライフタイムを管理するハンドラ。</returns>
        /// <exception cref="InvalidOperationException">このビルダーが Build によって消費済みの場合。</exception>
        public SystemFlowHandler<TContext> BuildSystemFlow<TSystemFlow>(params ScopeHandler[] parents)
            where TSystemFlow : ISystemFlow<TContext>
        {
            return BuildSystemFlowCore<TSystemFlow>(
                parents,
                scope => ActivatorUtilities.CreateInstance<TSystemFlow>(scope)
                    ?? throw new InvalidOperationException());
        }

        /// <summary>
        /// 現在のサービス登録と追加パラメーターを使用して SystemFlow を生成します。
        /// </summary>
        /// <typeparam name="TSystemFlow">生成する SystemFlow の型。</typeparam>
        /// <param name="parameters">SystemFlow の生成時に DI へ追加で渡すコンストラクタ引数。</param>
        /// <param name="parents">SystemFlow 用スコープで解決できないサービスを探索する親スコープ。</param>
        /// <returns>生成された SystemFlow を保持し、専用スコープのライフタイムを管理するハンドラ。</returns>
        /// <exception cref="InvalidOperationException">このビルダーが Build によって消費済みの場合。</exception>
        public SystemFlowHandler<TContext> BuildSystemFlow<TSystemFlow>(object[] parameters, params ScopeHandler[] parents)
            where TSystemFlow : ISystemFlow<TContext>
        {
            return BuildSystemFlowCore<TSystemFlow>(
                parents,
                scope => ActivatorUtilities.CreateInstance<TSystemFlow>(scope, parameters)
                    ?? throw new InvalidOperationException());
        }

        private SystemFlowHandler<TContext> BuildSystemFlowCore<TSystemFlow>(
            ScopeHandler[] parents,
            Func<ScopeHandler, TSystemFlow> createSystemFlow)
            where TSystemFlow : ISystemFlow<TContext>
        {
            var scope = BuildScope(parents);

            try
            {
                var systemFlow = createSystemFlow(scope);
                return new SystemFlowHandler<TContext>(scope, systemFlow);
            }
            catch (Exception creationException)
            {
                try
                {
                    scope.Dispose();
                }
                catch (Exception disposalException)
                {
                    throw new AggregateException(creationException, disposalException);
                }

                throw;
            }
        }
    }
}
