using System;
using System.Collections.Generic;

namespace InteractionFlow.Core.Entities
{
    /// <summary>
    /// 任意の値を保持し、必要に応じて型指定で取り出せる基本ラッパーです。
    /// </summary>
    /// <typeparam name="TValue">ラップする値の型。</typeparam>
    /// <param name="value">初期値。</param>
    public abstract class Entry<TValue>(TValue? value) : IEntry, IDisposable
    {
        /// <summary>
        /// ラップしている値を取得します。
        /// </summary>
        public TValue? Value { get; protected set; } = value;

        /// <summary>
        /// 保持している値を指定した型として取得します。
        /// </summary>
        /// <typeparam name="T">取得する値の型。</typeparam>
        /// <returns>
        /// 保持値が <typeparamref name="T"/> として取得できる場合は成功結果。
        /// 保持値が Entry の場合は、その Entry を再帰的に解決した結果。
        /// 保持値がない場合、または指定型として取得できない場合は失敗結果。
        /// </returns>
        public Result<T> Parse<T>()
        {
            if (Value == null)
            {
                return new NullReferenceException(nameof(Value));
            }
            else if (Value is T t)
            {
                return t;
            }
            else if (Value is IEntry entry)
            {
                return entry.Parse<T>();
            }
            else
            {
                return new ArgumentException(nameof(T));
            }

        }

        /// <summary>
        /// ラップしている値が破棄可能な場合は破棄し、値を解放します。
        /// </summary>
        public void Dispose()
        {
            if (Value != null && Value is IDisposable disposable)
            {
                disposable.Dispose();
                Value = default;
            }

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// ラップしている値の文字列表現を取得します。
        /// </summary>
        /// <returns>値の文字列表現。値がない場合は (Null)。</returns>
        public override string? ToString()
        {
            return Value?.ToString() ?? "(Null)";
        }

        /// <summary>
        /// 他の Entry が保持する値と等しいかを判定します。
        /// </summary>
        /// <param name="other">比較対象の Entry。</param>
        /// <returns>保持値が等しい場合は <see langword="true"/>。</returns>
        public bool ValueEqualsTo(Entry<TValue> other)
        {
            if (Value == null && other.Value == null)
            {
                return true;
            }

            if (Value == null || other.Value == null)
            {
                return false;
            }

            return EqualityComparer<TValue>.Default.Equals(Value, other.Value);
        }

        /// <summary>
        /// 指定された値と保持値が等しいかを判定します。
        /// </summary>
        /// <param name="other">比較対象の値。</param>
        /// <returns>保持値が等しい場合は <see langword="true"/>。</returns>
        public bool ValueEqualsTo(TValue? other)
        {
            if (Value == null && other == null)
            {
                return true;
            }

            if (Value == null || other == null)
            {
                return false;
            }

            return EqualityComparer<TValue>.Default.Equals(Value, other);
        }
    }
}
