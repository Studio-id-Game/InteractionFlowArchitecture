using System;

namespace InteractionFlow.Core.Entities
{
    public static class ResultExtensions
    {
        /// <summary>
        /// Result型 への暗黙的キャストを明示的に呼び出します。
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static Result AsResult(this Exception e)
        {
            return e;
        }

        /// <summary>
        /// 成功時に副作用を実行し、そのまま伝播します。
        /// </summary>
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
        /// 成功時の値をもとに次のResultを生成し、失敗はそのまま伝播します。
        /// </summary>
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
        /// 成功時の値をもとに次のResultを生成し、失敗はそのまま伝播します。
        /// </summary>
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
        /// 失敗時の値をもとに次のResultを生成し、成功はそのまま伝播します。
        /// </summary>
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
