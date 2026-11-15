using InteractionFlow.Core.ResultAPI.Attributes;
using System;

namespace InteractionFlow.Core.ResultAPI;

/// <summary>
/// Result 拡張メソッドの生成時に置換する型とメンバーを定義します。
/// </summary>
[ResultAPITemplate]
public abstract class ResultTemplateBase
{
    /// <summary>
    /// 受け手の Result 型を表します。
    /// </summary>
    protected class Self
    {
        /// <summary>
        /// 受け手の成功判定に置換されます。
        /// </summary>
        /// <exception cref="NotSupportedException">プレースホルダーを直接参照しました。</exception>
        public bool IsSuccess => throw new NotSupportedException();
    }

    /// <summary>
    /// 変換先の Result 型を表します。
    /// </summary>
    protected class Other
    {
        /// <summary>
        /// 受け手の成功値を使う変換先のファクトリ呼び出しに置換されます。
        /// </summary>
        /// <exception cref="NotSupportedException">プレースホルダーを直接呼び出しました。</exception>
        public static Other CreateSuccess(Self value) => throw new NotSupportedException();

        /// <summary>
        /// 受け手の失敗情報を使う変換先のファクトリ呼び出しに置換されます。
        /// </summary>
        /// <exception cref="NotSupportedException">プレースホルダーを直接呼び出しました。</exception>
        public static Other CreateFailure(Self self) => throw new NotSupportedException();
    }

    /// <summary>
    /// 戻り値のある成功時コールバックを表します。
    /// </summary>
    protected class OnSuccessFunc<T>
    {
        /// <summary>
        /// 成功時コールバックの呼び出しに置換されます。
        /// </summary>
        /// <exception cref="NotSupportedException">プレースホルダーを直接呼び出しました。</exception>
        public T Invoke(Self self) => throw new NotSupportedException();
    }

    /// <summary>
    /// 戻り値のある失敗時コールバックを表します。
    /// </summary>
    protected class OnErrorFunc<T>
    {
        /// <summary>
        /// 失敗時コールバックの呼び出しに置換されます。
        /// </summary>
        /// <exception cref="NotSupportedException">プレースホルダーを直接呼び出しました。</exception>
        public T Invoke(Self self) => throw new NotSupportedException();
    }

    /// <summary>
    /// 戻り値のない成功時コールバックを表します。
    /// </summary>
    protected class OnSuccessAction
    {
        /// <summary>
        /// 成功時コールバックの呼び出しに置換されます。
        /// </summary>
        /// <exception cref="NotSupportedException">プレースホルダーを直接呼び出しました。</exception>
        public void Invoke(Self self) => throw new NotSupportedException();
    }

    /// <summary>
    /// 戻り値のない失敗時コールバックを表します。
    /// </summary>
    protected class OnErrorAction
    {
        /// <summary>
        /// 失敗時コールバックの呼び出しに置換されます。
        /// </summary>
        /// <exception cref="NotSupportedException">プレースホルダーを直接呼び出しました。</exception>
        public void Invoke(Self self) => throw new NotSupportedException();
    }
}
