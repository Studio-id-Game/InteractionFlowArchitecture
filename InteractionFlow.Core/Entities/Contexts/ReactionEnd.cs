using System;

namespace InteractionFlow.Core.Entities.Contexts
{
    /// <summary>
    /// Reaction が生成するフロー終了結果を表します。
    /// </summary>
    public sealed class ReactionEnd
    {
        internal static ReactionEnd Success { get; } = new(null);

        internal ReactionEnd(Exception? exception)
        {
            Exception = exception;
        }

        /// <summary>
        /// 終了結果に例外が設定されているかどうかを取得します。
        /// </summary>
        public bool HasException => Exception != null;

        /// <summary>
        /// 終了結果にキャンセル例外が設定されているかどうかを取得します。
        /// </summary>
        public bool HasCanceled => Exception is OperationCanceledException;

        /// <summary>
        /// 終了結果に設定されている例外を取得します。
        /// </summary>
        public Exception? Exception { get; }
    }
}
