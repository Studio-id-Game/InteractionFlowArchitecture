using System;

namespace InteractionFlow.Core.Entities
{
    public static class ResultTExtensions
    {
        /// <summary>
        /// 成功時に副作用を実行し、結果はそのまま返します。
        /// </summary>
        public static Result<T> OnSuccess<T>(
            this Result<T> result,
            Action<T> action)
        {
            if (result.Try(out var value, out _))
                action(value);
            return result;
        }

        /// <summary>
        /// 失敗時に副作用を実行し、結果はそのまま返します。
        /// </summary>
        public static Result<T> OnError<T>(
            this Result<T> result,
            Action<Exception> action)
        {
            if (!result.Try(out _, out var error))
                action(error);
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
        /// 成功時の値を取得し、失敗時は指定されたデフォルト値を返します。
        /// </summary>
        public static T OrDefault<T>(
            this Result<T> result,
            T defaultValue)
        {
            if (result.Try(out var value, out _))
                return value;
            else
                return defaultValue;
        }
    }
}
