using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts.ReactionPorts;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.ExternalPorts.ReactionPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.Interactions
{
    /// <summary>
    /// コンソールへ文字列を出力する標準 Interaction です。
    /// </summary>
    /// <param name="exception">通常の例外をフロー終了時の反応へ変換するポート。</param>
    /// <param name="cancellation">キャンセルをフロー終了時の反応へ変換するポート。</param>
    /// <param name="consoleWrite">コンソール出力に使用する Reaction ポート。</param>
    /// <param name="dependency">この Interaction が明示的に依存するフローノード。</param>
    public class ConsoleWriting(
        IExceptionPort<Exception> exception,
        ICancellationPort cancellation,
        IConsoleWriter consoleWrite,
        params IDependencyNode[] dependency)
        : InteractionOptionalArg<(ConsoleOutput?, ConsoleState?)>(exception, cancellation, [consoleWrite, .. dependency])
    {
        /// <summary>
        /// 出力内容と出力状態の既定オプションを取得します。
        /// </summary>
        protected override (ConsoleOutput?, ConsoleState?) DefaultOption => (DefaultOutput, DefaultState);

        /// <summary>
        /// 出力内容が指定されない場合の既定出力を取得します。
        /// </summary>
        protected virtual ConsoleOutput DefaultOutput => new("Default ConsoleWrite Text.");

        /// <summary>
        /// 出力状態が指定されない場合の既定状態を取得します。
        /// </summary>
        protected virtual ConsoleState DefaultState => ConsoleState.Default;

        /// <summary>
        /// 指定された出力オプションでコンソール出力を実行します。
        /// </summary>
        /// <param name="context">Interaction に渡すフローコンテキスト。</param>
        /// <param name="option">出力内容と出力状態のオプション。</param>
        /// <returns>出力後のフロー終了結果。</returns>
        protected override async Task<ReactionEnd> ExecuteCoreAsync(IFlowContext context, (ConsoleOutput?, ConsoleState?) option)
        {
            return await InteractWithUserAsyncCore(context, option).ConfigureAwait(false);
        }

        /// <summary>
        /// オプションまたはコンテキストから出力内容と状態を決定し、コンソールへ書き込みます。
        /// </summary>
        /// <param name="context">Interaction に渡すフローコンテキスト。</param>
        /// <param name="option">出力内容と出力状態のオプション。</param>
        /// <returns>出力後のフロー終了結果。</returns>
        protected virtual async Task<ReactionEnd> InteractWithUserAsyncCore(IFlowContext context, (ConsoleOutput?, ConsoleState?) option)
        {
            var output = option.Item1 ?? (context.TryGet<ConsoleOutput>(out var _output) ? _output! : DefaultOutput);
            var state = option.Item2 ?? (context.TryGet<ConsoleState>(out var _state) ? _state! : DefaultState);

            using var scope = consoleWrite.GetStateScope();
            scope.State = state;
            return await consoleWrite.Write(context, output).ConfigureAwait(false);
        }
    }
}
