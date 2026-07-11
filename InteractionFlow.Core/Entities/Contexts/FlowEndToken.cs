using System;

namespace InteractionFlow.Core.Entities.Contexts
{
    /// <summary>
    /// フローの終了時点のコンテキストと、終了時に発生した例外情報を保持します。
    /// </summary>
    public sealed class FlowEndToken
    {
        /// <summary>
        /// フロー終了時点で最後に扱われていたコンテキストを取得します。
        /// </summary>
        public IFlowContext LastContext { get; }

        /// <summary>
        /// フロー終了時に発生した例外を取得または設定します。
        /// </summary>
        public Exception? Exception { get; set; }

        /// <summary>
        /// フロー終了時の例外をキャンセル例外として取得または設定します。
        /// </summary>
        public OperationCanceledException? CanceledException
        {
            get => Exception as OperationCanceledException;
            set => Exception = value;
        }

        /// <summary>
        /// 例外が設定されているかどうかを取得します。
        /// </summary>
        public bool HasException => Exception != null;

        /// <summary>
        /// 設定されている例外がキャンセル例外かどうかを取得します。
        /// </summary>
        public bool HasCanceled => HasException && Exception is OperationCanceledException;

        internal FlowEndToken(IFlowContext lastContext)
        {
            LastContext = lastContext;
        }

        internal FlowEndToken NormalizeLastContext(IFlowContext context)
        {
            if (LastContext == context)
            {
                return this;
            }

            return new FlowEndToken(context)
            {
                Exception = Exception
            };
        }
    }
}
