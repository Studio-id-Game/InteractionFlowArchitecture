using System;

namespace InteractionFlow.Core.Entities
{
    public static class ResultExtensions
    {
        /// <summary>
        /// 成功時に副作用を実行し、結果はそのまま返します。
        /// </summary>
        public static Result OnSuccess(
            this Result result,
            Action action)
        {
            if (result.IsValid)
                action();

            return result;
        }

        /// <summary>
        /// 失敗時に副作用を実行し、結果はそのまま返します。
        /// </summary>
        public static Result OnError(
            this Result result,
            Action<Exception> action)
        {
            if (!result.Try(out var e))
                action(e);
            return result;
        }

        /// <summary>
        /// 成功・失敗をそれぞれのハンドラで処理し、単一の結果に変換します。
        /// </summary>
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
    }
}
