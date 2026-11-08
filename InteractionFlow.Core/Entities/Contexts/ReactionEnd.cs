using System;

namespace InteractionFlow.Core.Entities.Contexts
{
    /// <summary>Reaction が生成する終了結果を表します。</summary>
    public sealed class ReactionEnd
    {
        internal static ReactionEnd Success { get; } = new(Result.Success);

        internal ReactionEnd(Result result)
        {
            Result = result;
        }

        /// <summary>Reaction の成功・失敗を取得します。</summary>
        public Result Result { get; }

        /// <summary>終了結果が失敗かどうかを取得します。</summary>
        public bool HasException => !Result.Try(out _);

        /// <summary>失敗の直接の原因がキャンセル例外かどうかを取得します。</summary>
        /// <remarks>集約例外の内部は探索しません。</remarks>
        public bool HasCanceled => Exception is OperationCanceledException;

        /// <summary>ResultException のラッパーを除いた失敗原因を取得します。</summary>
        /// <remarks>内部例外を持たない ResultException はそのまま返します。</remarks>
        public Exception? Exception => Result.Try(out var error) ? null : error.InnerException ?? error;
    }
}
