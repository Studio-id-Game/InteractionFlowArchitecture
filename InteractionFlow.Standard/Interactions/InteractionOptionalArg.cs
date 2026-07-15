using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts.ReactionPorts;
using InteractionFlow.Core.Interactions;
using System;
using System.Threading;
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
        params IDependencyNode[] dependency)
        : Interaction(exceptionPort, cancellationPort, dependency)
    {
        private sealed class OptionScope(TOption? value)
        {
            public TOption? Value { get; } = value;
        }

        // 明示 option 付き実行でも Core の ExecuteAsync を通すため、現在の async flow に option を保持する。
        private readonly AsyncLocal<OptionScope?> currentOption = new();

        /// <summary>
        /// オプションを指定せず実行した場合に使用する既定値を取得します。
        /// </summary>
        protected virtual TOption? DefaultOption => default;

        /// <summary>
        /// 既定のオプションを使用して Interaction を実行します。
        /// </summary>
        /// <param name="context">Interaction に渡すフローコンテキスト。</param>
        /// <returns>Interaction の終了結果。</returns>
        protected sealed override Task<ReactionEnd> ExecuteCoreAsync(IFlowContext context)
        {
            var option = currentOption.Value;
            return ExecuteCoreAsync(context, option == null ? DefaultOption : option.Value);
        }

        /// <summary>
        /// 指定されたオプションで Interaction を実行します。
        /// </summary>
        /// <param name="context">Interaction に渡すフローコンテキスト。</param>
        /// <param name="option">実行時に渡すオプション。</param>
        /// <returns>Interaction の終了結果。</returns>
        public async Task<FlowEndToken> ExecuteAsync(IFlowContext context, TOption? option)
        {
            var previousOption = currentOption.Value;
            currentOption.Value = new(option);

            try
            {
                return await ExecuteAsync(context).ConfigureAwait(false);
            }
            finally
            {
                currentOption.Value = previousOption;
            }
        }

        /// <summary>
        /// 指定されたオプションで Interaction の本体を実行します。
        /// </summary>
        /// <param name="context">Interaction に渡すフローコンテキスト。</param>
        /// <param name="option">実行時に渡すオプション。</param>
        /// <returns>Reaction が生成した終了結果。</returns>
        protected abstract Task<ReactionEnd> ExecuteCoreAsync(IFlowContext context, TOption? option);
    }
}
