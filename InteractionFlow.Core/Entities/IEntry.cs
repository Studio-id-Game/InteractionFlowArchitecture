using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace InteractionFlow.Core.Entities
{
    /// <summary>
    /// 型指定による値の解決を提供する Entry の内部契約です。
    /// </summary>
    internal interface IEntry
    {
        /// <summary>
        /// 保持している値を指定した型として取得します。
        /// </summary>
        /// <typeparam name="T">取得する値の型。</typeparam>
        /// <returns>値を指定型として取得できる場合は成功結果。取得できない場合は失敗結果。</returns>
        public Result<T> Parse<T>();

        /// <summary>
        /// 訪問済み Entry を参照しながら、保持している値を指定した型として取得します。
        /// </summary>
        /// <typeparam name="T">取得する値の型。</typeparam>
        /// <param name="reportMissing">未発見も例外情報として返すか。</param>
        /// <param name="visitedEntries">循環参照の検出に使用する訪問済み Entry。</param>
        /// <param name="value">取得した値。</param>
        /// <param name="error">解決失敗。未発見で reportMissing が false の場合は null。</param>
        /// <returns>値を取得できた場合は true。</returns>
        internal bool TryResolve<T>(bool reportMissing, ISet<IEntry>? visitedEntries,
            [MaybeNullWhen(false)] out T value, out Exception? error);
    }
}
