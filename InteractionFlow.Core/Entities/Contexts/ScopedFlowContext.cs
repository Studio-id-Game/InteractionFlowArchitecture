using System;
using System.Collections.Generic;

namespace InteractionFlow.Core.Entities.Contexts
{
    /// <summary>
    /// 既存のコンテキストに一時的な文脈値を重ねて扱うコンテキストです。
    /// </summary>
    /// <param name="parentContext">値の探索先となる元のコンテキスト。</param>
    public sealed class ScopedFlowContext(IFlowContext parentContext) : IFlowContext
    {
        private readonly List<object> values = [];

        /// <summary>
        /// 元のコンテキストに紐づくキャンセル制御オブジェクトを取得します。
        /// </summary>
        public CancellationObject Cancellation => parentContext.Cancellation;

        /// <summary>
        /// 一時的な文脈値を追加します。
        /// </summary>
        /// <typeparam name="T">追加する値の型。</typeparam>
        /// <param name="value">追加する値。<see langword="null"/> は指定できません。</param>
        /// <returns>現在のスコープ付きコンテキスト。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> が <see langword="null"/> の場合。</exception>
        public ScopedFlowContext With<T>(T value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            values.Insert(0, value);
            return this;
        }

        /// <summary>
        /// 追加された一時文脈値を新しい順に探索し、見つからない場合は元のコンテキストから値を取得します。
        /// </summary>
        /// <typeparam name="T">取得する値の型。</typeparam>
        /// <param name="value">取得できた値。取得できない場合は既定値。</param>
        /// <returns>値を取得できた場合は <see langword="true"/>、取得できない場合は <see langword="false"/>。</returns>
        public bool TryGet<T>(out T? value)
        {
            foreach (var item in values)
            {
                if (item is T matched)
                {
                    value = matched;
                    return true;
                }
            }

            return parentContext.TryGet(out value);
        }
    }
}
