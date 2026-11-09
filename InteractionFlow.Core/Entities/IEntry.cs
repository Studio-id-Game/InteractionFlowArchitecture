using System;
using System.Diagnostics.CodeAnalysis;

namespace InteractionFlow.Core.Entities
{
    /// <summary>Entry の内部値解決契約です。</summary>
    internal interface IEntry
    {
        /// <summary>反復解決で調べる保持値。</summary>
        object? UntypedValue { get; }

        /// <summary>利用者の展開回数に含めない Context 内部の格納ラッパーか。</summary>
        bool IsContextBox => false;

        /// <summary>未発見の診断情報も含めた公開の解決結果。</summary>
        Result<T> Parse<T>();

        /// <summary>通常の未発見と、展開上限による解決失敗を区別して返します。</summary>
        bool TryResolve<T>(bool reportMissing,
            [MaybeNullWhen(false)] out T value, out Exception? error);
    }
}
