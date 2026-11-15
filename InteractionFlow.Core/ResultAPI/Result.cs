using InteractionFlow.Core.Entities;
using InteractionFlow.Core.ResultAPI.Attributes;
using System;
using System.Diagnostics.CodeAnalysis;

namespace InteractionFlow.Core.ResultAPI;

/// <summary>
/// 値を持たない処理結果を表し、成功または例外による失敗を保持します。
/// <see langword="default"/> の <see cref="Result"/> は成功として扱われます。
/// </summary>
[ResultAPIResult]
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
    /// trueの時、操作や状態の成功を意味します
    /// </summary>
    public bool IsSuccess => Error == default;

    /// <summary>
    /// 失敗の原因となった例外
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
    /// <param name="Error"></param>
    /// <returns></returns>
    public static Result CreateFailure(Exception Error)
    {
        if (Error is null) throw new ArgumentNullException(nameof(Error));
        return new(Error);
    }

    /// <summary>
    /// 成功結果を作成します。
    /// </summary>
    /// <returns></returns>
    public static Result CreateSuccess() => default;

    /// <summary>
    /// 成功を表す結果を取得します。
    /// </summary>
    public static Result Success { get; } = CreateSuccess();

    /// <summary>
    /// 例外を失敗結果へ変換します。
    /// </summary>
    /// <param name="exception">失敗として保持する例外。</param>
    public static implicit operator Result(Exception exception) => new(exception);
}

/// <summary>
/// 値を持つ処理結果を表し、成功時の非 null の値または例外による失敗を保持します。
/// <see langword="default"/> の <see cref="Result{TValue}"/> は未初期化のため操作時に例外を送出します。
/// また、<see langword="null"/> と <see cref="Exception"/> 派生型の値は成功値として使用できません。
/// </summary>
/// <typeparam name="T">成功時に保持する値の型。</typeparam>
[ResultAPIResult]
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
    /// trueの時、操作や状態の成功を意味します
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
    /// 失敗の原因となった例外
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
    /// 成功時の値
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
    /// 値を取り除いた成功または失敗の結果を取得します。
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
    /// <exception cref="ResultException">この結果が失敗を表している場合に発生します。</exception>
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
    /// <param name="value">成功時に保持されている値。失敗時は <see langword="null"/>。</param>
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
    /// <param name="Error"></param>
    /// <returns></returns>
    public static Result<T> CreateFailure(Exception Error) => new(Error);

    /// <summary>
    /// 値から成功結果を作成します。
    /// </summary>
    /// <param name="Value"></param>
    /// <returns></returns>
    public static Result<T> CreateSuccess(T Value) => new(Value);

    /// <summary>
    /// 非 null の値を成功結果へ変換します。
    /// </summary>
    /// <param name="value">成功結果として保持する値。<see langword="null"/> と <see cref="Exception"/> 派生型の値は指定できません。</param>
    /// <exception cref="ResultException">
    /// <paramref name="value"/> が <see langword="null"/>、または <see cref="Exception"/> 派生型の場合に発生します。
    /// </exception>
    public static implicit operator Result<T>(T value) => new(value);

    /// <summary>
    /// 例外を失敗結果へ変換します。
    /// </summary>
    /// <param name="exception">失敗として保持する例外。<typeparamref name="T"/> が <see cref="Exception"/> 派生型の場合も、成功値ではなく失敗として扱います。</param>
    public static implicit operator Result<T>(Exception exception) => new(exception);
}

/// <summary>
/// 
/// </summary>
/// <typeparam name="T"></typeparam>
/// <typeparam name="TError"></typeparam>
[ResultAPIResult]
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

    /// <summary>
    /// trueの時、操作や状態の成功を意味します
    /// </summary>
    public bool IsSuccess => result.IsSuccess;

    /// <summary>
    /// 失敗の原因となった例外
    /// </summary>
    [ResultAPICallbackArgument(onSuccess: false)]
    public ResultException? Error => result.Error;

    /// <summary>
    /// 成功時の値
    /// </summary>
    [ResultAPICallbackArgument(onSuccess: true)]
    public T? Value => result.Value;

    /// <summary>
    /// 失敗時の値
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

    /// <inheritdoc cref="Result{T}.WithoutValue"/>
    public readonly Result WithoutValue => result.WithoutValue;

    /// <summary>
    /// 失敗値を取り除いた成功または失敗の結果を取得します。
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
    /// <param name="errorValue">失敗時に保持されている失敗値。成功時は <see langword="null"/>。</param>
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
    /// <param name="Error"></param>
    /// <param name="ErrorValue"></param>
    /// <returns></returns>
    public static Result<T, TError> CreateFailure(Exception Error, TError ErrorValue) => new(Error, ErrorValue);

    /// <summary>
    /// 値から成功結果を作成します。
    /// </summary>
    /// <param name="Value"></param>
    /// <returns></returns>
    public static Result<T, TError> CreateSuccess(T Value) => new(Value);

    /// <summary>
    /// 非 null の値を成功結果へ変換します。
    /// </summary>
    /// <param name="value">成功結果として保持する値。<see langword="null"/> と <see cref="Exception"/> 派生型の値は指定できません。</param>
    /// <exception cref="ResultException">
    /// <paramref name="value"/> が <see langword="null"/>、または <see cref="Exception"/> 派生型の場合に発生します。
    /// </exception>
    public static implicit operator Result<T, TError>(T value) => new(value);

    /// <summary>
    /// 例外とエラー値のタプルを失敗結果へ変換します。
    /// </summary>
    public static implicit operator Result<T, TError>((Exception exception, TError errorValue) error) => new(error.exception, error.errorValue);
}
