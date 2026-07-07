using System;
using System.Threading.Tasks;

namespace InteractionFlow.Core.Entities
{
    public static class ResultAsyncExtensions
    {
        /// <summary>
        /// 非同期フローを開始します。
        /// </summary>
        public static Task<Result> StartAsync(this Result result)
        {
            return Task.FromResult(result);
        }

        /// <summary>
        /// 成功・失敗をそれぞれのハンドラで処理し、単一の値に変換します。
        /// </summary>
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
        /// 成功時の値をもとに次のResultを生成し、失敗はそのまま伝播します。
        /// </summary>
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
        /// 成功時の値をもとに次のResultを生成し、失敗はそのまま伝播します。
        /// </summary>
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
        /// 失敗時の値をもとに次のResultを生成し、成功はそのまま伝播します。
        /// </summary>
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
