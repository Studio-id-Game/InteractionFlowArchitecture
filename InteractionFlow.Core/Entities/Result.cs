using System;
using System.Diagnostics.CodeAnalysis;

namespace InteractionFlow.Core.Entities
{
    /// <summary>
    /// 値を持たない処理結果を表し、成功または例外による失敗を保持します。
    /// </summary>
    /// <remarks>
    /// <see langword="default"/> の <see cref="Result"/> は成功として扱われます。
    /// </remarks>
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
    /// 値を持つ処理結果を表し、成功時の非 null の値または例外による失敗を保持します。
    /// </summary>
    /// <remarks>
    /// <see langword="default"/> の <see cref="Result{TValue}"/> は未初期化のため失敗として扱われます。
    /// また、<see langword="null"/> と <see cref="Exception"/> 派生型の値は成功値として使用できません。
    /// <see cref="Exception"/> 派生型の値を成功値として扱いたい場合は、コンテナ型でラップしてください。
    /// </remarks>
    /// <typeparam name="TValue">成功時に保持する値の型。</typeparam>
    public readonly struct Result<TValue>
    {
        private readonly TValue? value;
        private readonly Result result;
        private readonly bool hasValue;

        private Result(TValue value)
        {
            if (value is null)
            {
                throw new ResultException($"{nameof(Result<TValue>)} cannot contain a null success value.");
            }

            if (value is Exception exception)
            {
                throw new ResultException($"{nameof(Exception)} cannot be used as a successful {nameof(Result<TValue>)} value.", exception);
            }

            this.value = value;
            result = Result.Success;
            hasValue = true;
        }

        private Result(Exception exception)
        {
            value = default;
            result = exception ?? throw new ArgumentNullException(nameof(exception));
            hasValue = false;
        }

        /// <summary>
        /// 値を取り除いた成功または失敗の結果を取得します。未初期化の結果は失敗として扱います。
        /// </summary>
        public readonly Result WithoutValue
        {
            get
            {
                if (!result.Try(out _))
                {
                    return result;
                }

                return hasValue ? result : new ResultException($"{nameof(Result<TValue>)} is not initialized.");
            }
        }

        /// <summary>
        /// 結果が成功かどうかを判定し、成功時の値または失敗時の例外を取得します。
        /// </summary>
        /// <param name="value">成功時に保持されている値。失敗時または未初期化時は既定値。</param>
        /// <param name="e">失敗時または未初期化時に保持または生成される例外。成功時は <see langword="null"/>。</param>
        /// <returns>非 null の成功値を保持している場合は <see langword="true"/>、失敗または未初期化の場合は <see langword="false"/>。</returns>
        public readonly bool Try([MaybeNullWhen(false)] out TValue value, [MaybeNullWhen(true)] out ResultException e)
        {
            if (result.Try(out e))
            {
                if (hasValue)
                {
                    value = this.value!;
                    return true;
                }
                else
                {
                    value = default;
                    e = new ResultException($"{nameof(Result<TValue>)} is not initialized.");
                    return false;
                }
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
            WithoutValue.ThrowIfError();
        }

        /// <summary>
        /// 非 null の値を成功結果へ変換します。
        /// </summary>
        /// <param name="value">成功結果として保持する値。<see langword="null"/> と <see cref="Exception"/> 派生型の値は指定できません。</param>
        /// <exception cref="ResultException">
        /// <paramref name="value"/> が <see langword="null"/>、または <see cref="Exception"/> 派生型の場合に発生します。
        /// </exception>
        public static implicit operator Result<TValue>(TValue value) => new(value);

        /// <summary>
        /// 例外を失敗結果へ変換します。
        /// </summary>
        /// <param name="exception">失敗として保持する例外。<typeparamref name="TValue"/> が <see cref="Exception"/> 派生型の場合も、成功値ではなく失敗として扱います。</param>
        public static implicit operator Result<TValue>(Exception exception) => new(exception);
    }
}
