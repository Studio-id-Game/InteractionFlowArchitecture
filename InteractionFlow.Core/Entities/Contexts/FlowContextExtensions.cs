using System;
using System.Runtime.CompilerServices;

namespace InteractionFlow.Core.Entities.Contexts
{
    /// <summary>フローコンテキストの文脈値取得を補助します。</summary>
    public static class FlowContextExtensions
    {
        /// <summary>指定された型の必須文脈値を取得します。</summary>
        /// <typeparam name="T">要求する文脈値の型。</typeparam>
        /// <param name="context">値を探索するコンテキスト。</param>
        /// <param name="memberName">呼び出し元のメンバー名。通常はコンパイラーが設定します。</param>
        /// <param name="filePath">呼び出し元のソースファイルパス。通常はコンパイラーが設定します。</param>
        /// <param name="lineNumber">呼び出し元の行番号。通常はコンパイラーが設定します。</param>
        /// <returns>コンテキストから取得した値。</returns>
        /// <remarks>
        /// 探索は TryGet に委譲します。TryGet 自体が送出した例外は変換せず伝播します。
        /// 任意値には TryGet を使用してください。
        /// </remarks>
        /// <exception cref="ArgumentNullException">context が null の場合。</exception>
        /// <exception cref="RequiredContextValueNotFoundException">要求した値が見つからない場合。</exception>
        public static T GetRequired<T>(
            this IFlowContext context,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));
            if (context.TryGet<T>(out var value)) return value;
            throw new RequiredContextValueNotFoundException(typeof(T), memberName, filePath, lineNumber);
        }
    }
}
