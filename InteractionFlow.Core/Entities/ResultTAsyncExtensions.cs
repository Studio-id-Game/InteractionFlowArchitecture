using System;
using System.Threading.Tasks;

namespace InteractionFlow.Core.Entities
{

    /// <summary>
    /// 非同期の値付き <see cref="Result{TValue}"/> チェーンを構築するための拡張メソッドを提供します。
    /// </summary>
    public static class ResultTAsyncExtensions
    {
        /// <summary>
        /// 非同期フローを開始します。
        /// </summary>
        /// <typeparam name="T">成功結果が保持する値の型。</typeparam>
        /// <param name="result">開始時点の結果。</param>
        /// <returns>指定された結果を返す完了済みタスク。</returns>
        public static Task<Result<T>> StartAsync<T>(this Result<T> result)
        {
            return Task.FromResult(result);
        }

        /// <summary>
        /// 成功・失敗をそれぞれのハンドラで処理し、単一の値に変換します。
        /// </summary>
        /// <typeparam name="T">成功結果が保持する値の型。</typeparam>
        /// <typeparam name="U">変換後の値の型。</typeparam>
        /// <param name="task">解決する非同期結果。</param>
        /// <param name="onSuccess">成功時の値を受け取って呼び出す非同期変換処理。</param>
        /// <param name="onFailure">失敗時に例外を受け取って呼び出す非同期変換処理。</param>
        /// <returns>成功または失敗ハンドラが返した値。</returns>
        public static async Task<U> ResolveAsync<T, U>(
            this Task<Result<T>> task,
            Func<T, Task<U>> onSuccess,
            Func<Exception, Task<U>> onFailure)
        {
            if ((await task.ConfigureAwait(false)).Try(out var value, out var error))
                return await onSuccess(value).ConfigureAwait(false);
            else
                return await onFailure(error).ConfigureAwait(false);
        }

        /// <summary>
        /// 成功時の値をもとに値付きの次の非同期 <see cref="Result{TValue}"/> を生成し、失敗はそのまま伝播します。
        /// </summary>
        /// <typeparam name="T">連結元の成功結果が保持する値の型。</typeparam>
        /// <typeparam name="U">次の成功結果が保持する値の型。</typeparam>
        /// <param name="task">連結元の非同期結果。</param>
        /// <param name="binder">成功時の値を受け取って次の非同期結果を生成する処理。</param>
        /// <returns>成功時は <paramref name="binder"/> の結果、失敗時は元の失敗結果。</returns>
        public static async Task<Result<U>> ThenAsync<T, U>(
            this Task<Result<T>> task,
            Func<T, Task<Result<U>>> binder)
        {
            if ((await task.ConfigureAwait(false)).Try(out var value, out var error))
                return await binder(value).ConfigureAwait(false);
            else
                return error;
        }

        /// <summary>
        /// 成功時の値をもとに次の非同期 <see cref="Result"/> を生成し、失敗はそのまま伝播します。
        /// </summary>
        /// <typeparam name="T">成功結果が保持する値の型。</typeparam>
        /// <param name="task">連結元の非同期結果。</param>
        /// <param name="binder">成功時の値を受け取って次の非同期結果を生成する処理。</param>
        /// <returns>成功時は <paramref name="binder"/> の結果、失敗時は元の失敗結果。</returns>
        public static async Task<Result> ThenAsync<T>(
            this Task<Result<T>> task,
            Func<T, Task<Result>> binder)
        {
            if ((await task.ConfigureAwait(false)).Try(out var value, out var error))
                return await binder(value).ConfigureAwait(false);
            else
                return error;
        }

        /// <summary>
        /// 失敗時に例外を受け取って次の非同期 <see cref="Result{TValue}"/> を生成し、成功はそのまま伝播します。
        /// </summary>
        /// <typeparam name="T">成功結果が保持する値の型。</typeparam>
        /// <param name="task">連結元の非同期結果。</param>
        /// <param name="binder">失敗時に例外を受け取って次の非同期結果を生成する処理。</param>
        /// <returns>成功時は元の成功値、失敗時は <paramref name="binder"/> の結果。</returns>
        public static async Task<Result<T>> ThenErrorAsync<T>(
            this Task<Result<T>> task,
            Func<Exception, Task<Result<T>>> binder)
        {
            if ((await task.ConfigureAwait(false)).Try(out var value, out var error))
                return value;
            else
                return await binder(error).ConfigureAwait(false);
        }
    }
}
