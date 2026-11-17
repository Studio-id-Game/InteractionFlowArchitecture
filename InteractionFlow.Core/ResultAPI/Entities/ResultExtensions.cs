using InteractionFlow.Core.Entities;
using InteractionFlow.Core.ResultAPI.Entities.Attributes;
using System;
using ResultTypes = InteractionFlow.Core.ResultAPI.Entities;

namespace InteractionFlow.Core.ResultAPI.Entities;

/// <summary>
/// コードジェネレーターで生成される、同期版のResult拡張メソッドの生成先です
/// </summary>
[ResultAPIExtension(template: typeof(ResultTemplate))]
public static partial class ResultExtensions
{
    /// <summary>
    /// 値を <see cref="ResultTypes.Result{T}"/> の成功結果へ変換します。
    /// </summary>
    /// <typeparam name="T">成功結果が保持する値の型。</typeparam>
    /// <param name="value">成功結果として保持する値。<see langword="null"/> と <see cref="Exception"/> 派生型の値は指定できません。</param>
    /// <returns>値を保持した成功結果。</returns>
    /// <exception cref="ResultException">
    /// <paramref name="value"/> が <see langword="null"/>、または <see cref="Exception"/> 派生型の場合。
    /// </exception>
    public static ResultTypes.Result<T> AsResult<T>(this T value) => ResultTypes.Result<T>.CreateSuccess(value);
}
