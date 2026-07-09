using System;

namespace InteractionFlow.Core.Entities.Contexts
{
    /// <summary>
    /// フローコンテキスト内で更新可能な値を保持します。
    /// </summary>
    /// <typeparam name="T">保持する値の型。</typeparam>
    /// <param name="value">初期値。</param>
    public sealed class FlowContextValue<T>(T value) : IFlowContextValue
    {
        /// <summary>
        /// 現在保持している値を取得または設定します。
        /// </summary>
        public T Value { get; set; } = value;

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
        /// 指定された値が保持型に代入可能な場合、保持値を更新します。
        /// </summary>
        /// <typeparam name="T1">設定する値の型。</typeparam>
        /// <param name="value">設定する値。</param>
        /// <returns>値を更新できた場合は <see langword="true"/>。</returns>
        public bool TrySet<T1>(T1? value)
        {
            if (value != null && value is T valueT)
            {
                Value = valueT;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 指定された関数が保持型の値を生成できる場合、その結果で保持値を更新します。
        /// </summary>
        /// <typeparam name="T1">生成する値の型。</typeparam>
        /// <param name="select">設定する値を生成する関数。</param>
        /// <returns>値を更新できた場合は <see langword="true"/>。</returns>
        public bool TrySet<T1>(Func<T1?> select)
        {
            if (select is Func<T?> selectT)
            {
                var value = selectT();
                if (value != null)
                {
                    Value = value;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
    }
}
