using System;
using System.Threading.Tasks;

namespace InteractionFlow.Core.Entities
{
    /// <summary>
    /// 非同期の値なし <see cref="Result"/> チェーンを構築するための拡張メソッドを提供します。
    /// </summary>
    public static class ResultAsyncExtensions
    {
        /// <summary>
        /// 非同期フローを開始します。
        /// </summary>
        /// <param name="result">開始時点の結果。</param>
        /// <returns>指定された結果を返す完了済みタスク。</returns>
        public static Task<Result> StartAsync(this Result result)
        {
            return Task.FromResult(result);
        }

        /// <summary>
        /// 成功・失敗をそれぞれのハンドラで処理し、単一の値に変換します。
        /// </summary>
        /// <typeparam name="U">変換後の値の型。</typeparam>
        /// <param name="task">解決する非同期結果。</param>
        /// <param name="onSuccess">成功時に呼び出す非同期変換処理。</param>
        /// <param name="onFailure">失敗時に例外を受け取って呼び出す非同期変換処理。</param>
        /// <returns>成功または失敗ハンドラが返した値。</returns>
        public static async Task<U> ResolveAsync<U>(
            this Task<Result> task,
            Func<Task<U>> onSuccess,
            Func<Exception, Task<U>> onFailure)
        {
            if ((await task).Try(out var error))
                return await onSuccess();
            else
                return await onFailure(error);
        }

        /// <summary>
        /// 成功時に値付きの次の非同期 <see cref="Result{TValue}"/> を生成し、失敗はそのまま伝播します。
        /// </summary>
        /// <typeparam name="U">次の成功結果が保持する値の型。</typeparam>
        /// <param name="task">連結元の非同期結果。</param>
        /// <param name="binder">成功時に次の非同期結果を生成する処理。</param>
        /// <returns>成功時は <paramref name="binder"/> の結果、失敗時は元の失敗結果。</returns>
        public static async Task<Result<U>> ThenAsync<U>(
            this Task<Result> task,
            Func<Task<Result<U>>> binder)
        {
            if ((await task).Try(out var error))
                return await binder();
            else
                return error;
        }

        /// <summary>
        /// 成功時に次の非同期 <see cref="Result"/> を生成し、失敗はそのまま伝播します。
        /// </summary>
        /// <param name="task">連結元の非同期結果。</param>
        /// <param name="binder">成功時に次の非同期結果を生成する処理。</param>
        /// <returns>成功時は <paramref name="binder"/> の結果、失敗時は元の失敗結果。</returns>
        public static async Task<Result> ThenAsync(
            this Task<Result> task,
            Func<Task<Result>> binder)
        {
            if ((await task).Try(out var error))
                return await binder();
            else
                return error;
        }

        /// <summary>
        /// 失敗時に例外を受け取って次の非同期 <see cref="Result"/> を生成し、成功はそのまま伝播します。
        /// </summary>
        /// <param name="task">連結元の非同期結果。</param>
        /// <param name="binder">失敗時に例外を受け取って次の非同期結果を生成する処理。</param>
        /// <returns>成功時は成功結果、失敗時は <paramref name="binder"/> の結果。</returns>
        public static async Task<Result> ThenErrorAsync(
            this Task<Result> task,
            Func<Exception, Task<Result>> binder)
        {
            if ((await task).Try(out var error))
                return Result.Success;
            else
                return await binder(error);
        }
    }
}
