using System;

namespace InteractionFlow.Core.Entities.Contexts
{
    /// <summary>
    /// Reaction の結果に基づき、単独または SystemFlow で合成された相互作用の完了を表します。
    /// </summary>
    public sealed class InteractionEnd
    {
        /// <summary>
        /// Reaction が生成したフロー終了結果を取得します。
        /// </summary>
        public ReactionEnd End { get; }

        /// <summary>フローの成功・失敗を取得します。</summary>
        public Result Result => End.Result;

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

        internal InteractionEnd(ReactionEnd end)
        {
            End = end ?? throw new ArgumentNullException(nameof(end));
        }
    }
}
