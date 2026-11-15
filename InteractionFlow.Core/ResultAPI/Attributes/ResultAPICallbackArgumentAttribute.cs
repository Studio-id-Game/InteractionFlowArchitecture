using System;

namespace InteractionFlow.Core.ResultAPI.Attributes;

/// <summary>
/// <see cref="ResultAPIResultAttribute"/> が付いた型で、コールバック引数と同名のプロパティを対応付けます。
/// </summary>
/// <param name="onSuccess"><see langword="true"/> は成功時、<see langword="false"/> は失敗時です。</param>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class ResultAPICallbackArgumentAttribute(bool onSuccess) : Attribute
{
    /// <summary>
    /// 成功時のコールバック引数の候補かどうかを取得します。
    /// </summary>
    public bool OnSuccess { get; } = onSuccess;

    /// <summary>
    /// 失敗時のコールバック引数の候補かどうかを取得します。
    /// </summary>
    public bool OnError => !OnSuccess;
}
