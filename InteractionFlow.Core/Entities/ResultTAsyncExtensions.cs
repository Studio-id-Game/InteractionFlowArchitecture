using System;
using System.Threading.Tasks;

namespace InteractionFlow.Core.Entities
{

    public static class ResultTAsyncExtensions
    {
        /// <summary>
        /// 非同期フローを開始します。
        /// </summary>
        public static Task<Result<T>> StartAsync<T>(this Result<T> result)
        {
            return Task.FromResult(result);
        }

        /// <summary>
        /// 成功・失敗をそれぞれのハンドラで処理し、単一の値に変換します。
        /// </summary>
        public static async Task<U> ResolveAsync<T, U>(
            this Task<Result<T>> task,
            Func<T, Task<U>> onSuccess,
            Func<Exception, Task<U>> onFailure)
        {
            if ((await task).Try(out var value, out var error))
                return await onSuccess(value);
            else
                return await onFailure(error);
        }

        /// <summary>
        /// 成功時の値をもとに次のResultを生成し、失敗はそのまま伝播します。
        /// </summary>
        public static async Task<Result<U>> ThenAsync<T, U>(
            this Task<Result<T>> task,
            Func<T, Task<Result<U>>> binder)
        {
            if ((await task).Try(out var value, out var error))
                return await binder(value);
            else
                return error;
        }

        /// <summary>
        /// 成功時の値をもとに次のResultを生成し、失敗はそのまま伝播します。
        /// </summary>
        public static async Task<Result> ThenAsync<T>(
            this Task<Result<T>> task,
            Func<T, Task<Result>> binder)
        {
            if ((await task).Try(out var value, out var error))
                return await binder(value);
            else
                return error;
        }

        /// <summary>
        /// 失敗時の値をもとに次のResultを生成し、成功はそのまま伝播します。
        /// </summary>
        public static async Task<Result<T>> ThenErrorAsync<T>(
            this Task<Result<T>> task,
            Func<Exception, Task<Result<T>>> binder)
        {
            if ((await task).Try(out var value, out var error))
                return value;
            else
                return await binder(error);
        }
    }
}
