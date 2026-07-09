using System;
using System.Diagnostics.CodeAnalysis;

namespace InteractionFlow.Core.Entities
{
    /// <summary>
    /// 値を持たない処理結果を表し、成功または例外による失敗を保持します。
    /// </summary>
    public readonly struct Result
    {
        private readonly ResultException? exception;

        private Result(Exception? exception)
        {
            if (exception is null)
            {
                this.exception = null;
            }
            else
            {
                if (exception is ResultException resultException)
                {
                    this.exception = resultException;
                }
                else
                {
                    this.exception = new ResultException(exception);
                }
            }
        }

        /// <summary>
        /// 結果が成功かどうかを判定し、失敗時は保持している例外を取得します。
        /// </summary>
        /// <param name="e">失敗時に保持されている例外。成功時は <see langword="null"/>。</param>
        /// <returns>成功の場合は <see langword="true"/>、失敗の場合は <see langword="false"/>。</returns>
        public readonly bool Try([MaybeNullWhen(true)] out ResultException e)
        {
            if (exception == null)
            {
                e = default;
                return true;
            }
            else
            {
                e = exception;
                return false;
            }
        }

        /// <summary>
        /// 失敗結果の場合、保持している例外を送出します。
        /// </summary>
        /// <exception cref="ResultException">この結果が失敗を表している場合に発生します。</exception>
        public readonly void ThrowIfError()
        {
            if (exception != null)
                throw exception;
        }

        /// <summary>
        /// 成功を表す結果を取得します。
        /// </summary>
        public static Result Success { get; } = new(null);

        /// <summary>
        /// 例外を失敗結果へ変換します。
        /// </summary>
        /// <param name="exception">失敗として保持する例外。</param>
        public static implicit operator Result(Exception exception) => new(exception);
    }

    /// <summary>
    /// 値を持つ処理結果を表し、成功時の値または例外による失敗を保持します。
    /// </summary>
    /// <typeparam name="TValue">成功時に保持する値の型。</typeparam>
    public readonly struct Result<TValue>
    {
        private readonly TValue? value;
        private readonly Result result;

        private Result(TValue value)
        {
            this.value = value;
            result = Result.Success;
        }

        private Result(Exception exception)
        {
            value = default;
            result = exception ?? throw new ArgumentNullException(nameof(exception));
        }

        /// <summary>
        /// 値を取り除いた成功または失敗の結果を取得します。
        /// </summary>
        public readonly Result WithoutValue => result;

        /// <summary>
        /// 結果が成功かどうかを判定し、成功時の値または失敗時の例外を取得します。
        /// </summary>
        /// <param name="value">成功時に保持されている値。失敗時は既定値。</param>
        /// <param name="e">失敗時に保持されている例外。成功時は <see langword="null"/>。</param>
        /// <returns>成功の場合は <see langword="true"/>、失敗の場合は <see langword="false"/>。</returns>
        public readonly bool Try([MaybeNullWhen(false)] out TValue value, [MaybeNullWhen(true)] out ResultException e)
        {
            if (result.Try(out e))
            {
                value = this.value!;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        /// <summary>
        /// 失敗結果の場合、保持している例外を送出します。
        /// </summary>
        /// <exception cref="ResultException">この結果が失敗を表している場合に発生します。</exception>
        public readonly void ThrowIfError()
        {
            result.ThrowIfError();
        }

        /// <summary>
        /// 値を成功結果へ変換します。
        /// </summary>
        /// <param name="value">成功結果として保持する値。</param>
        public static implicit operator Result<TValue>(TValue value) => new(value);

        /// <summary>
        /// 例外を失敗結果へ変換します。
        /// </summary>
        /// <param name="exception">失敗として保持する例外。</param>
        public static implicit operator Result<TValue>(Exception exception) => new(exception);
    }
}
