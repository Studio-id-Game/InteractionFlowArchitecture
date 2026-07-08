using System;

namespace InteractionFlow.Core.Entities.Contexts
{
    /// <summary>
    /// フローコンテキスト上で型をキーとして値を取得・更新する要素を表します。
    /// </summary>
    public interface IFlowContextValue
    {
        /// <summary>
        /// 指定した型として値を取得します。
        /// </summary>
        /// <typeparam name="T">取得する値の型。</typeparam>
        /// <param name="value">取得できた値。取得できない場合は既定値。</param>
        /// <returns>値を取得できた場合は <see langword="true"/>、取得できない場合は <see langword="false"/>。</returns>
        public bool TryGet<T>(out T? value);

        /// <summary>
        /// 指定した型の値で保持値を更新します。
        /// </summary>
        /// <typeparam name="T">設定する値の型。</typeparam>
        /// <param name="value">設定する値。</param>
        /// <returns>値を設定できた場合は <see langword="true"/>、設定できない場合は <see langword="false"/>。</returns>
        public bool TrySet<T>(T? value);

        /// <summary>
        /// 指定した型の値を生成する関数で保持値を更新します。
        /// </summary>
        /// <typeparam name="T">設定する値の型。</typeparam>
        /// <param name="select">設定する値を生成する関数。</param>
        /// <returns>値を設定できた場合は <see langword="true"/>、設定できない場合は <see langword="false"/>。</returns>
        public bool TrySet<T>(Func<T> select);
    }
}
