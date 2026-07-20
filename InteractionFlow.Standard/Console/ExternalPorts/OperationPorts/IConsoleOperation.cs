using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts.OperationPorts;
using InteractionFlow.Standard.Console.Entities;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.Console.ExternalPorts.OperationPorts
{
    /// <summary>
    /// コンソールからユーザー入力を取得する Operation ポートを表します。
    /// </summary>
    public interface IConsoleOperation : IOperationPort, IHasFunctionState<ConsoleOperationState>
    {
        /// <summary>
        /// 実際のコンソール入力を待たず、あらかじめ設定された値を返すテスト用 Operation ポートを表します。
        /// </summary>
        public interface IDummy : IConsoleOperation
        {
            /// <summary>
            /// ダミーの文字列入力を取得または設定します。
            /// </summary>
            ConsoleInputText DummyText { get; set; }

            /// <summary>
            /// ダミーのキー入力を取得または設定します。
            /// </summary>
            ConsoleInputKeyInfo DummyKeyInfo { get; set; }

            /// <summary>
            /// ダミー入力を返すまでの待機時間を取得または設定します。
            /// </summary>
            int InputDelayTime { get; set; }
        }

        /// <summary>
        /// ユーザーが入力した文字列を非同期に取得します。
        /// </summary>
        /// <param name="context">入力操作に使用するフローコンテキスト。</param>
        /// <returns>入力された文字列。</returns>
        ValueTask<ConsoleInputText> WaitUserTextAsync(IFlowContext context);

        /// <summary>
        /// ユーザーが入力したキーを非同期に取得します。
        /// </summary>
        /// <param name="context">入力操作に使用するフローコンテキスト。</param>
        /// <returns>入力されたキー情報。</returns>
        ValueTask<ConsoleInputKeyInfo> WaitUserKeyAsync(IFlowContext context);

        /// <summary>
        /// ユーザーが入力したキーを、表示有無を指定して非同期に取得します。
        /// </summary>
        /// <param name="context">入力操作に使用するフローコンテキスト。</param>
        /// <param name="hideChar">入力文字を表示しない場合は <see langword="true"/>。</param>
        /// <returns>入力されたキー情報。</returns>
        ValueTask<ConsoleInputKeyInfo> WaitUserKeyAsync(IFlowContext context, bool hideChar);
    }
}
