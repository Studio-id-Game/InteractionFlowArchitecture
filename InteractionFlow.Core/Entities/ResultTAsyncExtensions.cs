using System;
using System.Threading.Tasks;

namespace InteractionFlow.Core.Entities
{

    /// <summary>
    /// 非同期の値付き <see cref="Result{TValue}"/> チェーンを構築するための拡張メソッドを提供します。
    /// </summary>
    /// <remarks>
    /// コールバックやタスクが送出する例外・キャンセルを捕捉して Result 化しません。
    /// Result の失敗は各操作の契約に従って伝播または処理します。
    /// </remarks>
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
        /// 値を <see cref="Result{TValue}"/> の非同期の成功結果へ変換します。
        /// </summary>
        /// <typeparam name="T">成功結果が保持する値の型。</typeparam>
        /// <param name="value">成功結果として保持する値。<see langword="null"/> と <see cref="Exception"/> 派生型の値は指定できません。</param>
        /// <returns>値を保持した成功結果。</returns>
        /// <exception cref="ResultException">
        /// <paramref name="value"/> が <see langword="null"/>、または <see cref="Exception"/> 派生型の場合。
        /// </exception>
        public static Task<Result<T>> AsResultAsync<T>(this T value)
        {
            return value.AsResult().StartAsync();
        }

        /// <summary>
        /// 非同期結果を待ち、成功値を別の値へ同期的に変換します。失敗はそのまま伝播します。
        /// </summary>
        /// <typeparam name="T">元の成功値の型。</typeparam>
        /// <typeparam name="U">変換後の成功値の型。</typeparam>
        /// <param name="task">変換元の非同期結果。</param>
        /// <param name="mapper">成功値を同期的に変換する関数。失敗時は呼び出しません。</param>
        /// <returns>変換した値の成功結果、または元の失敗結果。</returns>
        /// <remarks>
        /// タスクや変換関数の例外・キャンセルは捕捉しません。
        /// 戻り値は AsResult と同じ成功値の制約に従います。
        /// Result を返す非同期変換には ThenAsync を使用してください。
        /// </remarks>
        public static async Task<Result<U>> MapAsync<T, U>(
            this Task<Result<T>> task,
            Func<T, U> mapper)
        {
            if ((await task.ConfigureAwait(false)).Try(out var value, out var error))
                return mapper(value).AsResult();
            else
                return error;
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
