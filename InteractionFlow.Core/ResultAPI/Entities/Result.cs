using InteractionFlow.Core.Entities;
using InteractionFlow.Core.ResultAPI.Entities.Attributes;
using System;
using System.Diagnostics.CodeAnalysis;

namespace InteractionFlow.Core.ResultAPI.Entities;

/// <summary>
/// 値なしの処理結果です。<see langword="default"/> は成功です。
/// </summary>
public readonly struct Result
{
    private Result(Exception? error)
    {
        if (error is null)
        {
            Error = null;
        }
        else
        {
            if (error is ResultException resultException)
            {
                Error = resultException;
            }
            else
            {
                Error = new ResultException(error);
            }
        }
    }

    /// <summary>
    /// 成功したかどうかを取得します。
    /// </summary>
    public bool IsSuccess => Error == default;

    /// <summary>
    /// 失敗時に保持する例外です。成功時は <see langword="null"/> です。
    /// </summary>
    [ResultAPICallbackArgument(onSuccess: false)]
    public ResultException? Error { get; }

    /// <summary>
    /// 失敗結果の場合、保持している例外を送出します。
    /// </summary>
    /// <exception cref="ResultException">この結果が失敗を表している場合に発生します。</exception>
    public readonly void ThrowIfError()
    {
        if (Error != null)
            throw Error;
    }

    /// <summary>
    /// 例外を保持しているかどうかを判定し、取得します。
    /// </summary>
    /// <param name="e">失敗時に保持されている例外。成功時は <see langword="null"/>。</param>
    /// <returns>例外を保持している場合は <see langword="true"/>、それ以外は <see langword="false"/>。</returns>
    public readonly bool HasError([MaybeNullWhen(false)] out ResultException e)
    {
        e = Error;
        return e != null;
    }

    /// <summary>
    /// エラーから失敗結果を作成します。
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="Error"/> が <see langword="null"/> です。</exception>
    public static Result CreateFailure(Exception Error)
    {
        if (Error is null) throw new ArgumentNullException(nameof(Error));
        return new(Error);
    }

    /// <summary>
    /// 成功結果を作成します。
    /// </summary>
    public static Result CreateSuccess() => default;

    /// <summary>
    /// 成功を表す結果を取得します。
    /// </summary>
    public static Result Success { get; } = CreateSuccess();

    /// <summary>
    /// <see cref="CreateFailure(Exception)"/> で失敗結果を作成します。
    /// </summary>
    public static implicit operator Result(Exception exception) => CreateFailure(exception);
}

/// <summary>
/// 値付きの処理結果です。<see langword="default"/> は未初期化で、操作すると <see cref="ResultException"/> を送出します。
/// </summary>
/// <typeparam name="T">成功時に保持する値の型。</typeparam>
public readonly struct Result<T>
{
    private readonly Result result;
    private readonly T? value;
    private readonly bool initialized;

    private Result(T value)
    {
        if (value is null)
        {
            throw new ResultException($"{nameof(Result<T>)} cannot contain a null success value.");
        }

        if (value is Exception exception)
        {
            throw new ResultException($"{nameof(Result<T>)} cannot contain a {nameof(Exception)} value.", exception);
        }

        result = Result.Success;
        this.value = value;
        initialized = true;
    }

    private Result(Exception error)
    {
        result = error ?? throw new ArgumentNullException(nameof(error));
        value = default;
        initialized = true;
    }

    /// <summary>
    /// 成功したかどうかを取得します。
    /// </summary>
    public bool IsSuccess
    {
        get
        {
            CheckInitialized();
            return result.IsSuccess;
        }
    }

    /// <summary>
    /// 失敗時に保持する例外です。成功時は <see langword="null"/> です。
    /// </summary>
    [ResultAPICallbackArgument(onSuccess: false)]
    public ResultException? Error
    {
        get
        {
            CheckInitialized();
            return result.Error;
        }
    }

    /// <summary>
    /// 成功時に保持する値です。失敗時は <c>default(T)</c> です。
    /// </summary>
    [ResultAPICallbackArgument(onSuccess: true)]
    public T? Value
    {
        get
        {
            CheckInitialized();
            return value!;
        }
    }

    /// <summary>
    /// 成功値を除き、結果の状態と例外を引き継ぎます。
    /// </summary>
    public readonly Result WithoutValue
    {
        get
        {
            CheckInitialized();
            return result;
        }
    }

    internal void CheckInitialized()
    {
        if (!initialized) throw new ResultException($"{nameof(Result<T>)} is not initialized.");
    }

    /// <summary>
    /// 失敗結果の場合、保持している例外を送出します。
    /// </summary>
    /// <exception cref="ResultException">結果が失敗または未初期化です。</exception>
    public readonly void ThrowIfError()
    {
        CheckInitialized();
        if (Error != null)
            throw Error;
    }

    /// <inheritdoc cref="Result.HasError(out ResultException)"/>
    public readonly bool HasError([MaybeNullWhen(false)] out ResultException e)
    {
        CheckInitialized();
        return result.HasError(out e);
    }

    /// <summary>
    /// 値を保持しているかどうかを判定し、取得します。
    /// </summary>
    /// <param name="value">成功時に保持する値。失敗時は <c>default(T)</c>。</param>
    /// <returns>値を保持している場合は <see langword="true"/>、それ以外は <see langword="false"/>。</returns>
    public readonly bool HasValue([MaybeNullWhen(false)] out T value)
    {
        CheckInitialized();
        value = this.value;
        return IsSuccess;
    }

    /// <summary>
    /// エラーから失敗結果を作成します。
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="Error"/> が <see langword="null"/> です。</exception>
    public static Result<T> CreateFailure(Exception Error) => new(Error);

    /// <summary>
    /// 値から成功結果を作成します。
    /// </summary>
    /// <exception cref="ResultException">
    /// <paramref name="Value"/> が <see langword="null"/> または <see cref="Exception"/> のインスタンスです。
    /// </exception>
    public static Result<T> CreateSuccess(T Value) => new(Value);

    /// <summary>
    /// <see cref="CreateSuccess(T)"/> で成功結果を作成します。
    /// </summary>
    public static implicit operator Result<T>(T value) => CreateSuccess(value);

    /// <summary>
    /// <see cref="CreateFailure(Exception)"/> で失敗結果を作成します。
    /// </summary>
    public static implicit operator Result<T>(Exception exception) => CreateFailure(exception);
}

/// <summary>
/// 値付きの処理結果です。失敗時は例外と失敗値を保持します。
/// <see langword="default"/> は未初期化で、操作すると <see cref="ResultException"/> を送出します。
/// </summary>
/// <typeparam name="T">成功値の型。</typeparam>
/// <typeparam name="TError">失敗値の型。</typeparam>
public readonly struct Result<T, TError>
{
    private readonly Result<T> result;
    private readonly TError? errorValue;

    private Result(T value)
    {
        result = value;
        errorValue = default;
    }

    private Result(Exception? error, TError? errorValue)
    {
        if (errorValue is null)
        {
            throw new ResultException($"{nameof(Result<T, TError>)} cannot contain a null errorValue.");
        }

        if (errorValue is Exception exception)
        {
            throw new ResultException($"{nameof(Result<T, TError>)} cannot contain a {nameof(Exception)} errorValue.", exception);
        }

        result = error ?? throw new ArgumentNullException(nameof(error));
        this.errorValue = errorValue;
    }

    /// <inheritdoc cref="Result{T}.IsSuccess"/>
    public bool IsSuccess => result.IsSuccess;

    /// <inheritdoc cref="Result{T}.Error"/>
    [ResultAPICallbackArgument(onSuccess: false)]
    public ResultException? Error => result.Error;

    /// <inheritdoc cref="Result{T}.Value"/>
    [ResultAPICallbackArgument(onSuccess: true)]
    public T? Value => result.Value;

    /// <summary>
    /// 失敗時に保持する値です。成功時は <c>default(TError)</c> です。
    /// </summary>
    [ResultAPICallbackArgument(onSuccess: false)]
    public TError? ErrorValue
    {
        get
        {
            result.CheckInitialized();
            return errorValue;
        }
    }

    /// <summary>
    /// 成功値と失敗値を除き、結果の状態と例外を引き継ぎます。
    /// </summary>
    public readonly Result WithoutValue => result.WithoutValue;

    /// <summary>
    /// 失敗値を除き、成功値と例外を引き継ぎます。
    /// </summary>
    public readonly Result<T> WithoutErrorValue
    {
        get
        {
            result.CheckInitialized();
            return result;
        }
    }

    /// <inheritdoc cref="Result{T}.ThrowIfError()"/>
    public readonly void ThrowIfError()
    {
        result.ThrowIfError();
    }

    /// <inheritdoc cref="Result{T}.HasError(out ResultException)"/>
    public readonly bool HasError([MaybeNullWhen(false)] out ResultException e)
    {
        return result.HasError(out e);
    }

    /// <summary>
    /// 例外と失敗値を保持しているかどうかを判定し、取得します。
    /// </summary>
    /// <param name="e">失敗時に保持されている例外。成功時は <see langword="null"/>。</param>
    /// <param name="errorValue">失敗時に保持する失敗値。成功時は <c>default(TError)</c>。</param>
    /// <returns>例外と失敗値を保持している場合は <see langword="true"/>、それ以外は <see langword="false"/>。</returns>
    public readonly bool HasError([MaybeNullWhen(false)] out ResultException e, [MaybeNullWhen(false)] out TError errorValue)
    {
        errorValue = ErrorValue;
        return result.HasError(out e);
    }

    /// <inheritdoc cref="Result{T}.HasValue(out T)"/>
    public readonly bool HasValue([MaybeNullWhen(false)] out T value)
    {
        return result.HasValue(out value);
    }

    /// <summary>
    /// エラーとエラー値から失敗結果を作成します。
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="Error"/> が <see langword="null"/> です。</exception>
    /// <exception cref="ResultException">
    /// <paramref name="ErrorValue"/> が <see langword="null"/> または <see cref="Exception"/> のインスタンスです。
    /// </exception>
    public static Result<T, TError> CreateFailure(Exception Error, TError ErrorValue) => new(Error, ErrorValue);

    /// <inheritdoc cref="Result{T}.CreateSuccess(T)"/>
    public static Result<T, TError> CreateSuccess(T Value) => new(Value);

    /// <summary>
    /// <see cref="CreateSuccess(T)"/> で成功結果を作成します。
    /// </summary>
    public static implicit operator Result<T, TError>(T value) => CreateSuccess(value);

    /// <summary>
    /// タプルの例外と失敗値から <see cref="CreateFailure(Exception, TError)"/> で失敗結果を作成します。
    /// </summary>
    public static implicit operator Result<T, TError>((Exception exception, TError errorValue) error)
        => CreateFailure(error.exception, error.errorValue);
}
