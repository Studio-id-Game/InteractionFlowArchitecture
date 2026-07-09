using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts.ReactionPorts;
using InteractionFlow.Core.Interactions;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.Interactions
{
    /// <summary>
    /// オプション引数付きで実行できる Interaction の基底クラスです。
    /// </summary>
    /// <typeparam name="TOption">Interaction に渡すオプションの型。</typeparam>
    /// <param name="exceptionPort">通常の例外をフロー終了時の反応へ変換するポート。</param>
    /// <param name="cancellationPort">キャンセルをフロー終了時の反応へ変換するポート。</param>
    /// <param name="dependency">この Interaction が明示的に依存するフローノード。</param>
    public abstract class InteractionOptionalArg<TOption>(
        IExceptionPort<Exception> exceptionPort,
        ICancellationPort cancellationPort,
        params IFlowNode[] dependency)
        : Interaction(exceptionPort, cancellationPort, dependency)
    {
        /// <summary>
        /// オプションを指定せず実行した場合に使用する既定値を取得します。
        /// </summary>
        protected virtual TOption? DefaultOption => default;

        /// <summary>
        /// 既定のオプションを使用して Interaction を実行します。
        /// </summary>
        /// <param name="context">Interaction に渡すフローコンテキスト。</param>
        /// <returns>Interaction の終了結果。</returns>
        public sealed override Task<FlowEndToken> ExecuteAsync(IFlowContext context)
        {
            return ExecuteAsync(context, DefaultOption);
        }

        /// <summary>
        /// 指定されたオプションで Interaction を実行します。
        /// </summary>
        /// <param name="context">Interaction に渡すフローコンテキスト。</param>
        /// <param name="option">実行時に渡すオプション。</param>
        /// <returns>Interaction の終了結果。</returns>
        public abstract Task<FlowEndToken> ExecuteAsync(IFlowContext context, TOption? option);


        /// <summary>
        /// Interaction の実行本体を、ライブラリ標準の例外ハンドリング内で実行します。
        /// <para>
        /// <see cref="OperationCanceledException"/> およびその他の <see cref="Exception"/> は捕捉され、
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
