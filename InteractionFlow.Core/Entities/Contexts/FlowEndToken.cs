using System;

namespace InteractionFlow.Core.Entities.Contexts
{
    /// <summary>
    /// フローの終了結果と、実行に渡されたコンテキストを結合したトークンです。
    /// </summary>
    public sealed class FlowEndToken
    {
        /// <summary>
        /// 現在のフロー実行境界へ渡されたコンテキスト参照を取得します。
        /// </summary>
        /// <remarks>
        /// このトークンはコンテキストの所有権を取得せず、破棄しません。
        /// </remarks>
        public IFlowContext LastContext { get; }

        /// <summary>
        /// Reaction が生成したフロー終了結果を取得します。
        /// </summary>
        public ReactionEnd End { get; }

        /// <summary>
        /// フロー終了時に発生した例外を取得します。
        /// </summary>
        public Exception? Exception => End.Exception;

        /// <summary>
        /// フロー終了時の例外をキャンセル例外として取得します。
        /// </summary>
        public OperationCanceledException? CanceledException => Exception as OperationCanceledException;

        /// <summary>
        /// 例外が設定されているかどうかを取得します。
        /// </summary>
        public bool HasException => End.HasException;

        /// <summary>
        /// 設定されている例外がキャンセル例外かどうかを取得します。
        /// </summary>
        public bool HasCanceled => End.HasCanceled;

        internal FlowEndToken(IFlowContext lastContext, ReactionEnd end)
        {
            LastContext = lastContext ?? throw new ArgumentNullException(nameof(lastContext));
            End = end ?? throw new ArgumentNullException(nameof(end));
        }
    }
}
