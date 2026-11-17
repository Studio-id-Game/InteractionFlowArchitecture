using InteractionFlow.Core.ResultAPI.Entities.Attributes;
using System;

namespace InteractionFlow.Core.ResultAPI.Entities;

/// <summary>
/// コードジェネレーターで生成される、同期版のResult拡張メソッドの生成先です
/// </summary>
[ResultAPIExtension(template: typeof(ResultTemplate))]
public static partial class ResultExtensions
{
    /// <summary>
    /// 値を <see cref="Result{T}"/> の成功結果へ変換します。
    /// </summary>
    /// <typeparam name="T">成功結果が保持する値の型。</typeparam>
    /// <param name="value">成功結果として保持する値。<see langword="null"/> と <see cref="Exception"/> 派生型の値は指定できません。</param>
    /// <returns>値を保持した成功結果。</returns>
    /// <exception cref="ResultException">
    /// <paramref name="value"/> が <see langword="null"/>、または <see cref="Exception"/> 派生型の場合。
    /// </exception>
    public static global::InteractionFlow.Core.ResultAPI.Entities.Result<T> AsResult<T>(this T value)
        => global::InteractionFlow.Core.ResultAPI.Entities.Result<T>.CreateSuccess(value);
}
