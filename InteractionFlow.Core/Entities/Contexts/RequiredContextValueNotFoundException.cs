using System;
using System.IO;

namespace InteractionFlow.Core.Entities.Contexts
{
    /// <summary>コンテキストに必須の文脈値が見つからなかったことを表します。</summary>
    public sealed class RequiredContextValueNotFoundException : InvalidOperationException
    {
        /// <summary>要求した文脈値の型を指定して例外を作成します。</summary>
        /// <param name="requiredType">見つからなかった文脈値の型。</param>
        /// <param name="memberName">値を要求したメンバー名。未指定の場合は空文字列。</param>
        /// <param name="filePath">値を要求したソースファイルのパス。未指定の場合は空文字列。</param>
        /// <param name="lineNumber">値を要求した行番号。未指定の場合は 0。</param>
        /// <exception cref="ArgumentNullException">requiredType が null の場合。</exception>
        public RequiredContextValueNotFoundException(
            Type requiredType, string memberName = "", string filePath = "", int lineNumber = 0)
            : base(CreateMessage(requiredType, memberName, filePath, lineNumber))
        {
            RequiredType = requiredType;
            MemberName = memberName;
            FilePath = filePath;
            LineNumber = lineNumber;
        }

        /// <summary>見つからなかった文脈値の型を取得します。</summary>
        public Type RequiredType { get; }

        /// <summary>値を要求したメンバー名を取得します。</summary>
        public string MemberName { get; }

        /// <summary>値を要求したソースファイルのパスを取得します。</summary>
        public string FilePath { get; }

        /// <summary>値を要求した行番号を取得します。</summary>
        public int LineNumber { get; }

        private static string CreateMessage(Type requiredType, string memberName, string filePath, int lineNumber)
        {
            if (requiredType is null) throw new ArgumentNullException(nameof(requiredType));
            var message = $"Required context value of type '{requiredType.FullName ?? requiredType.Name}' was not found.";
            if (memberName.Length == 0 && filePath.Length == 0 && lineNumber == 0) return message;
            return $"{message} Requested at {Path.GetFileName(filePath)}:{lineNumber} ({memberName}).";
        }
    }
}
