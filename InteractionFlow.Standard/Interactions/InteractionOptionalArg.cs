using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Core.ReactionPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.Interactions
{
    public abstract class InteractionOptionalArg<TOption>(
        IExceptionPort<Exception> exceptionPort,
        ICancellationPort cancellationPort,
        params IFlowNode[] dependency)
        : Interaction(exceptionPort, cancellationPort, dependency)
    {
        protected virtual TOption? DefaultOption => default;

        public sealed override Task<FlowEndToken> ExecuteAsync(IFlowContext context)
        {
            return ExecuteAsync(context, DefaultOption);
        }

        public abstract Task<FlowEndToken> ExecuteAsync(IFlowContext context, TOption? option);


        /// <summary>
        /// Interaction の実行本体を、ライブラリ標準の例外ハンドリング内で実行します。
        /// <para>
        /// <see cref="OperationCanceledException"/> および <typeparamref name="TException"/> は捕捉され、
        /// <see cref="FlowEndToken"/> に変換されます。
        /// </para>
        /// <para>
        /// <paramref name="attachCancellation"/> が指定された場合、
        /// Task の cancellation 監視を追加します。
        /// この監視は cancellation 同期のみを目的としており、
        /// 例外処理は本メソッドの main await path 側で行われます。
        /// </para>
        /// </summary>
        /// <param name="context">
        /// Interaction の実行コンテキスト。
        /// </param>
        /// <param name="option">
        /// Interaction の実行オプション。
        /// </param>
        /// <param name="function">
        /// 実行する Interaction 本体。
        /// </param>
        /// <param name="attachCancellation">
        /// 非同期 cancellation 時に実行される追加処理。
        /// </param>
        /// <returns>
        /// Railway 変換後の <see cref="FlowEndToken"/> を返します。
        /// </returns>
        protected Task<FlowEndToken> TryCatchBlock(
            IFlowContext context,
            TOption? option,
            Func<IFlowContext, TOption?, Task<FlowEndToken>> function,
            Func<ValueTask>? attachCancellation = null)
        {
            return TryCatchBlockAsync(context, c => function(c, option), attachCancellation);
        }
    }
}
