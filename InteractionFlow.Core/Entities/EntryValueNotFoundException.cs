using System;

namespace InteractionFlow.Core.Entities
{
    /// <summary>
    /// Entry から要求された型の値を取得できない場合に使用する例外です。
    /// </summary>
    [Serializable]
    public class EntryValueNotFoundException : Exception
    {
        /// <summary>
        /// 新しい例外を作成します。
        /// </summary>
        public EntryValueNotFoundException() { }

        /// <summary>
        /// 指定したエラーメッセージを持つ新しい例外を作成します。
        /// </summary>
        /// <param name="message">エラー内容を説明するメッセージ。</param>
        public EntryValueNotFoundException(string message) : base(message) { }

        /// <summary>
        /// 指定したエラーメッセージと内部例外を持つ新しい例外を作成します。
        /// </summary>
        /// <param name="message">エラー内容を説明するメッセージ。</param>
        /// <param name="inner">現在の例外の原因となった例外。</param>
        public EntryValueNotFoundException(string message, Exception inner) : base(message, inner) { }
    }
}
