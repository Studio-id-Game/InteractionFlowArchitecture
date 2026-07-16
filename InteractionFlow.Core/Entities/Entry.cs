using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace InteractionFlow.Core.Entities
{
    /// <summary>
    /// 任意の値を保持し、必要に応じて型指定で取り出せる基本ラッパーです。
    /// </summary>
    /// <typeparam name="TValue">ラップする値の型。</typeparam>
    /// <param name="value">初期値。</param>
    public abstract class Entry<TValue>(TValue? value) : IEntry, IDisposable
    {
        private sealed class EntryReferenceEqualityComparer : IEqualityComparer<IEntry>
        {
            public static EntryReferenceEqualityComparer Instance { get; } = new();

            public bool Equals(IEntry? x, IEntry? y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(IEntry obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
            }
        }

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
        /// 保持値がない場合、指定型として取得できない場合、または Entry の循環参照を検出した場合は失敗結果。
        /// </returns>
        public Result<T> Parse<T>()
        {
            HashSet<IEntry> visitedEntries = new(EntryReferenceEqualityComparer.Instance);
            return ((IEntry)this).Parse<T>(visitedEntries);
        }

        Result<T> IEntry.Parse<T>(ISet<IEntry> visitedEntries)
        {
            if (Value == null)
            {
                return new EntryValueNotFoundException($"Entry value is null. Requested type: {typeof(T).FullName}.");
            }
            else if (!visitedEntries.Add(this))
            {
                return new InvalidOperationException($"Circular Entry reference was detected. Requested type: {typeof(T).FullName}. Entry type: {GetType().FullName}.");
            }
            else if (Value is T t)
            {
                return t;
            }
            else if (Value is IEntry entry)
            {
                return entry.Parse<T>(visitedEntries);
            }
            else
            {
                return new EntryValueNotFoundException($"Entry value type mismatch. Requested type: {typeof(T).FullName}. Actual type: {Value.GetType().FullName}.");
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
