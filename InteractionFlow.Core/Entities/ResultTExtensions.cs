using System;

namespace InteractionFlow.Core.Entities
{
    public static class ResultTExtensions
    {
        /// <summary>
        /// Result型 への暗黙的キャストを明示的に呼び出します。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Result<T> AsResult<T>(this T value)
        {
            return value;
        }

        /// <summary>
        /// 成功時に副作用を実行し、そのまま伝播します。
        /// </summary>
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
        /// 成功時の値をもとに次のResultを生成し、失敗はそのまま伝播します。
        /// </summary>
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
        /// 成功時の値をもとに次のResultを生成し、失敗はそのまま伝播します。
        /// </summary>
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
        /// 失敗時の値をもとに次のResultを生成し、成功はそのまま伝播します。
        /// </summary>
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
