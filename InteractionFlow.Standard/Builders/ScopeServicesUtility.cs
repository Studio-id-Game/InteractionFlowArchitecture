using InteractionFlow.Core.Builders;
using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Core.ProgramFlows;
using System.Runtime.CompilerServices;

namespace InteractionFlow.Standard.Builders
{
    /// <summary>
    /// Interaction Flow の層ごとの意図を明示してサービス登録する拡張メソッドを提供します。
    /// </summary>
    public static class ScopeServicesUtility
    {
        /// <summary>
        /// Interaction 実装を scoped サービスとして登録します。
        /// </summary>
        /// <typeparam name="TImplementation">登録する Interaction 実装型。</typeparam>
        /// <param name="this">登録先のサービス構成。</param>
        /// <returns>続けて登録を行うための現在のサービス構成。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IScopeServices UseInteraction<TImplementation>(this IScopeServices @this)
            where TImplementation : class, IInteraction
        {

            return @this.Use<TImplementation>();
        }

        /// <summary>
        /// ProgramFlow 実装を scoped サービスとして登録します。
        /// </summary>
        /// <typeparam name="TImplementation">登録する ProgramFlow 実装型。</typeparam>
        /// <param name="this">登録先のサービス構成。</param>
        /// <returns>続けて登録を行うための現在のサービス構成。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IScopeServices UseProgramFlow<TImplementation>(this IScopeServices @this)
            where TImplementation : class, IProgramFlow
        {

            return @this.Use<TImplementation>();
        }

        /// <summary>
        /// FunctionPort 実装を scoped サービスとして登録します。
        /// </summary>
        /// <typeparam name="TService">登録する FunctionPort サービス型。</typeparam>
        /// <typeparam name="TImplementation">サービスとして生成する実装型。</typeparam>
        /// <param name="this">登録先のサービス構成。</param>
        /// <returns>続けて登録を行うための現在のサービス構成。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IScopeServices UseFunction<TService, TImplementation>(this IScopeServices @this)
            where TService : class, IFlowNodeStateful
            where TImplementation : class, TService
        {
            return @this.Use<TService, TImplementation>();
        }
    }
}
