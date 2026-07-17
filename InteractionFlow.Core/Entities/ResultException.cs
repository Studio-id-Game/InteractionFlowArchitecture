using System;
using System.Diagnostics;

namespace InteractionFlow.Core.Entities
{
    /// <summary>
    /// <see cref="Result"/> または <see cref="Result{TValue}"/> が失敗や不正な成功値を表すために使用する例外です。
    /// </summary>
    public class ResultException : Exception
    {
        internal ResultException(Exception inner) : base($"ResultMessage : {inner.Message}", inner)
        {
        }

        internal ResultException(string message, Exception? inner = null) : base($"ResultMessage : {message}", inner)
        {
        }

        internal ResultException() : base($"ResultMessage : Invalid Result Exception")
        {

        }

# if DEBUG
        /// <summary>
        /// DEBUG ビルド時に、Result が作成された位置のスタックトレースを取得します。
        /// </summary>
        public StackTrace? ResultCreationStackTrace { get; } = new(3, true);
#else
        /// <summary>
        /// DEBUG ビルド時に、Result が作成された位置のスタックトレースを取得します。
        /// </summary>
        public StackTrace? ResultCreationStackTrace { get; } = null;
#endif
    }
}
