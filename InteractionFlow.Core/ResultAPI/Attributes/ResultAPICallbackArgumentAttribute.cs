using System;

namespace InteractionFlow.Core.ResultAPI.Attributes;

/// <summary>
/// プロパティをコールバック引数にします。
/// </summary>
/// <param name="onSuccess"><see langword="true"/> は成功時、<see langword="false"/> は失敗時です。</param>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class ResultAPICallbackArgumentAttribute(bool onSuccess) : Attribute
{
    /// <summary>コールバック引数の区分と並び順を指定します。</summary>
    /// <param name="onSuccess"><see langword="true"/> は成功時、<see langword="false"/> は失敗時です。</param>
    /// <param name="index">引数を並べる順序です。小さい値が先になります。</param>
    public ResultAPICallbackArgumentAttribute(bool onSuccess, int index) : this(onSuccess)
    {
        Index = index;
    }

    /// <summary>並び順です。同じ値の引数と、指定済みの引数に続く未指定分は、それぞれ名前の序数順に並びます。</summary>
    public int? Index { get; }

    /// <summary>
    /// 成功時のコールバック引数の候補かどうかを取得します。
    /// </summary>
    public bool OnSuccess { get; } = onSuccess;

    /// <summary>
    /// 失敗時のコールバック引数の候補かどうかを取得します。
    /// </summary>
    public bool OnError => !OnSuccess;
}
