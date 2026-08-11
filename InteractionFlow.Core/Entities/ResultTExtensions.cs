using System;

namespace InteractionFlow.Core.Entities
{
    /// <summary>
    /// 値を持つ <see cref="Result{TValue}"/> を連結・解決するための拡張メソッドを提供します。
    /// </summary>
    public static class ResultTExtensions
    {
        /// <summary>
        /// 値を <see cref="Result{TValue}"/> の成功結果へ変換します。
        /// </summary>
        /// <typeparam name="T">成功結果が保持する値の型。</typeparam>
        /// <param name="value">成功結果として保持する値。<see langword="null"/> と <see cref="Exception"/> 派生型の値は指定できません。</param>
        /// <returns>値を保持した成功結果。</returns>
        /// <exception cref="ResultException">
        /// <paramref name="value"/> が <see langword="null"/>、または <see cref="Exception"/> 派生型の場合。
        /// </exception>
        public static Result<T> AsResult<T>(this T value)
        {
            return value;
        }

        /// <summary>
        /// 成功時に副作用を実行し、そのまま伝播します。
        /// </summary>
        /// <typeparam name="T">成功結果が保持する値の型。</typeparam>
        /// <param name="result">判定する結果。</param>
        /// <param name="onSuccess">成功時の値を受け取って実行する処理。</param>
        /// <returns>元の結果。</returns>
        public static Result<T> OnSuccess<T>(
            this Result<T> result,
            Action<T> onSuccess)
        {
            if (result.Try(out var t, out _))
            {
                onSuccess(t);
            }

            return result;
        }

        /// <summary>
        /// 成功・失敗をそれぞれのハンドラで処理し、単一の値に変換します。
        /// </summary>
        /// <typeparam name="T">成功結果が保持する値の型。</typeparam>
        /// <typeparam name="U">変換後の値の型。</typeparam>
        /// <param name="result">解決する結果。</param>
        /// <param name="onSuccess">成功時の値を受け取って呼び出す変換処理。</param>
        /// <param name="onFailure">失敗時に例外を受け取って呼び出す変換処理。</param>
        /// <returns>成功または失敗ハンドラが返した値。</returns>
        public static U Resolve<T, U>(
            this Result<T> result,
            Func<T, U> onSuccess,
            Func<Exception, U> onFailure)
        {
            if (result.Try(out var value, out var error))
                return onSuccess(value);
            else
                return onFailure(error);
        }

        /// <summary>
        /// 成功時の値をもとに次の <see cref="Result"/> を生成し、失敗はそのまま伝播します。
        /// </summary>
        /// <typeparam name="T">成功結果が保持する値の型。</typeparam>
        /// <param name="result">連結元の結果。</param>
        /// <param name="binder">成功時の値を受け取って次の結果を生成する処理。</param>
        /// <returns>成功時は <paramref name="binder"/> の結果、失敗時は元の失敗結果。</returns>
        public static Result Then<T>(
            this Result<T> result,
            Func<T, Result> binder)
        {
            if (result.Try(out var value, out var error))
                return binder(value);
            else
                return error;
        }

        /// <summary>
        /// 成功時の値をもとに値付きの次の <see cref="Result{TValue}"/> を生成し、失敗はそのまま伝播します。
        /// </summary>
        /// <typeparam name="T">連結元の成功結果が保持する値の型。</typeparam>
        /// <typeparam name="U">次の成功結果が保持する値の型。</typeparam>
        /// <param name="result">連結元の結果。</param>
        /// <param name="binder">成功時の値を受け取って次の結果を生成する処理。</param>
        /// <returns>成功時は <paramref name="binder"/> の結果、失敗時は元の失敗結果。</returns>
        public static Result<U> Then<T, U>(
            this Result<T> result,
            Func<T, Result<U>> binder)
        {
            if (result.Try(out var value, out var error))
                return binder(value);
            else
                return error;
        }

        /// <summary>
        /// 失敗時に例外を受け取って次の <see cref="Result{TValue}"/> を生成し、成功はそのまま伝播します。
        /// </summary>
        /// <typeparam name="T">成功結果が保持する値の型。</typeparam>
        /// <param name="result">連結元の結果。</param>
        /// <param name="binder">失敗時に例外を受け取って次の結果を生成する処理。</param>
        /// <returns>成功時は元の成功値、失敗時は <paramref name="binder"/> の結果。</returns>
        public static Result<T> ThenError<T>(
            this Result<T> result,
            Func<Exception, Result<T>> binder)
        {
            if (result.Try(out var value, out var error))
                return value;
            else
                return binder(error);
        }
    }
}
