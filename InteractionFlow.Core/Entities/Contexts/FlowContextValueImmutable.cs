using System;

namespace InteractionFlow.Core.Entities.Contexts
{
    /// <summary>
    /// フローコンテキスト内で読み取り専用の値を保持します。
    /// </summary>
    /// <typeparam name="T">保持する値の型。</typeparam>
    /// <param name="value">保持する値。</param>
    public sealed class FlowContextValueImmutable<T>(T value) : IFlowContextValue
    {
        /// <summary>
        /// 保持している読み取り専用の値を取得します。
        /// </summary>
        public T Value { get; } = value;

        /// <summary>
        /// 保持値を指定された型として取得します。
        /// </summary>
        /// <typeparam name="T1">取得する値の型。</typeparam>
        /// <param name="value">取得できた値。型が一致しない場合は既定値。</param>
        /// <returns>保持値を指定型として取得できた場合は <see langword="true"/>。</returns>
        public bool TryGet<T1>(out T1? value)
        {
            if (Value is T1 valueT)
            {
                value = valueT;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        /// <summary>
        /// 読み取り専用のため、常に更新せず <see langword="false"/> を返します。
        /// </summary>
        /// <typeparam name="T1">設定を試みる値の型。</typeparam>
        /// <param name="value">設定を試みる値。</param>
        /// <returns>常に <see langword="false"/>。</returns>
        public bool TrySet<T1>(T1? value)
        {
            return false;
        }

        /// <summary>
        /// 読み取り専用のため、常に更新せず <see langword="false"/> を返します。
        /// </summary>
        /// <typeparam name="T1">設定を試みる値の型。</typeparam>
        /// <param name="select">設定を試みる値を生成する関数。</param>
        /// <returns>常に <see langword="false"/>。</returns>
        public bool TrySet<T1>(Func<T1?> select)
        {
            return false;
        }
    }
}
