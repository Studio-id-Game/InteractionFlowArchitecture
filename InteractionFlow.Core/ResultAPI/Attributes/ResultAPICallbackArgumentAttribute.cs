using System;

namespace InteractionFlow.Core.ResultAPI.Attributes;

/// <summary>
/// ResultAPIのコードジェネレータで利用するために、コールバックに渡す引数の候補をマークします。
/// <see cref="ResultAPIResultAttribute"/> でマークされたクラス・構造体内でのみ有効です。
/// </summary>
/// <param name="onSuccess"></param>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class ResultAPICallbackArgumentAttribute(bool onSuccess) : Attribute
{
    /// <summary>
    /// 成功時コールバックの候補であるか
    /// </summary>
    public bool OnSuccess { get; } = onSuccess;

    /// <summary>
    /// 失敗時コールバックの候補であるか
    /// </summary>
    public bool OnError => !OnSuccess;
}
