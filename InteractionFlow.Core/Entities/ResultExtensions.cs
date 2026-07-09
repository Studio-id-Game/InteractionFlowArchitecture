using System;

namespace InteractionFlow.Core.Entities
{
    /// <summary>
    /// 値を持たない <see cref="Result"/> を連結・解決するための拡張メソッドを提供します。
    /// </summary>
    public static class ResultExtensions
    {
        /// <summary>
        /// 例外を <see cref="Result"/> の失敗結果へ変換します。
        /// </summary>
        /// <param name="e">失敗として保持する例外。</param>
        /// <returns>例外を保持した失敗結果。</returns>
        public static Result AsResult(this Exception e)
        {
            return e;
        }

        /// <summary>
        /// 成功時に副作用を実行し、そのまま伝播します。
        /// </summary>
        /// <param name="result">判定する結果。</param>
        /// <param name="onSuccess">成功時に実行する処理。</param>
        /// <returns>元の結果。</returns>
        public static Result OnSuccess(
            this Result result,
            Action onSuccess)
        {
            if (result.Try(out _))
            {
                onSuccess();
            }

            return result;
        }

        /// <summary>
        /// 成功・失敗をそれぞれのハンドラで処理し、単一の結果に変換します。
        /// </summary>
        /// <typeparam name="T">変換後の値の型。</typeparam>
        /// <param name="result">解決する結果。</param>
        /// <param name="onSuccess">成功時に呼び出す変換処理。</param>
        /// <param name="onError">失敗時に例外を受け取って呼び出す変換処理。</param>
        /// <returns>成功または失敗ハンドラが返した値。</returns>
        public static T Resolve<T>(
            this Result result,
            Func<T> onSuccess,
            Func<Exception, T> onError)
        {
            if (result.Try(out var e))
                return onSuccess();
            else
                return onError(e);
        }

        /// <summary>
        /// 成功時に次の <see cref="Result"/> を生成し、失敗はそのまま伝播します。
        /// </summary>
        /// <param name="result">連結元の結果。</param>
        /// <param name="binder">成功時に次の結果を生成する処理。</param>
        /// <returns>成功時は <paramref name="binder"/> の結果、失敗時は元の失敗結果。</returns>
        public static Result Then(
            this Result result,
            Func<Result> binder)
        {
            if (result.Try(out var error))
                return binder();
            else
                return error;
        }

        /// <summary>
        /// 成功時に値付きの次の <see cref="Result{TValue}"/> を生成し、失敗はそのまま伝播します。
        /// </summary>
        /// <typeparam name="U">次の成功結果が保持する値の型。</typeparam>
        /// <param name="result">連結元の結果。</param>
        /// <param name="binder">成功時に次の結果を生成する処理。</param>
        /// <returns>成功時は <paramref name="binder"/> の結果、失敗時は元の失敗結果。</returns>
        public static Result<U> Then<U>(
            this Result result,
            Func<Result<U>> binder)
        {
            if (result.Try(out var error))
                return binder();
            else
                return error;
        }

        /// <summary>
        /// 失敗時に例外を受け取って次の <see cref="Result"/> を生成し、成功はそのまま伝播します。
        /// </summary>
        /// <param name="result">連結元の結果。</param>
        /// <param name="binder">失敗時に例外を受け取って次の結果を生成する処理。</param>
        /// <returns>成功時は成功結果、失敗時は <paramref name="binder"/> の結果。</returns>
        public static Result ThenError(
            this Result result,
            Func<Exception, Result> binder)
        {
            if (result.Try(out var error))
                return result;
            else
                return binder(error);
        }
    }
}
