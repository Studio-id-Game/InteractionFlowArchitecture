using InteractionFlow.Core.ResultAPI.Attributes;
using System;

namespace InteractionFlow.Core.ResultAPI;

/// <summary>
/// コードジェネレーターで生成される拡張メソッドのテンプレート基盤です。
/// ここで定義したクラスとメソッドは、ジェネレータによって解決・置換される特別なキーワードとして利用されます。
/// </summary>
[ResultAPITemplate]
public abstract class ResultTemplateBase
{
    /// <summary>
    /// 現在の値のダミー構文
    /// </summary>
    protected class Self
    {
        /// <summary>
        /// Resultの成功・失敗をチェックするダミー構文
        /// </summary>
        public bool IsSuccess => throw new NotSupportedException();
    }

    /// <summary>
    /// 結果値のダミー構文
    /// </summary>
    protected class Other
    {
        /// <summary>
        /// 現在の値から成功値を作成するダミー構文
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public static Other CreateSuccess(Self value) => throw new NotSupportedException();

        /// <summary>
        /// 現在の値から失敗値を作成するダミー構文
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public static Other CreateFailure(Self self) => throw new NotSupportedException();
    }

    /// <summary>
    /// 戻り値のある成功時コールバックのダミー構文
    /// </summary>
    /// <typeparam name="T"></typeparam>
    protected class OnSuccessFunc<T>
    {
        /// <summary>
        /// 戻り値のある成功時コールバックを実行するダミー構文
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public T Invoke(Self self) => throw new NotSupportedException();
    }

    /// <summary>
    /// 戻り値のある失敗時コールバックのダミー構文
    /// </summary>
    /// <typeparam name="T"></typeparam>
    protected class OnErrorFunc<T>
    {
        /// <summary>
        /// 戻り値のある失敗時コールバックを実行するダミー構文
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public T Invoke(Self self) => throw new NotSupportedException();
    }

    /// <summary>
    /// 成功時コールバックのダミー構文
    /// </summary>
    protected class OnSuccessAction
    {
        /// <summary>
        /// 成功時コールバックを実行するダミー構文
        /// </summary>
        /// <param name="self"></param>
        /// <exception cref="NotSupportedException"></exception>
        public void Invoke(Self self) => throw new NotSupportedException();
    }

    /// <summary>
    /// 失敗時コールバックのダミー構文
    /// </summary>
    protected class OnErrorAction
    {
        /// <summary>
        /// 失敗時コールバックを実行するダミー構文
        /// </summary>
        /// <param name="self"></param>
        /// <exception cref="NotSupportedException"></exception>
        public void Invoke(Self self) => throw new NotSupportedException();
    }
}
