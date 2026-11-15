using System;

namespace InteractionFlow.Core.ResultAPI.Attributes;

/// <summary>
/// ResultAPIのコードジェネレータで利用するために、コードの生成先をマークします。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class ResultAPIExtensionAttribute : Attribute
{
    /// <summary>
    /// 同期関数コンストラクタ
    /// </summary>
    /// <param name="template">生成内容を決めるテンプレートをタイプで指定します</param>
    public ResultAPIExtensionAttribute(Type template)
    {
        Template = template;
    }

    /// <summary>
    /// 非同期関数コンストラクタ。<see cref="ResultAPIFuncAttribute"/> のフラグと連携します。
    /// </summary>
    /// <param name="template">生成内容を決めるテンプレートをタイプで指定します</param>
    /// <param name="awaitableFuncType">Func系コールバックに利用するジェネリック型をタイプで指定します</param>
    /// <param name="awaitableActionType">Action系コールバックに利用するジェネリック型をタイプで指定します</param>
    /// <param name="suffix">関数名のサフィックスを指定します</param>
    public ResultAPIExtensionAttribute(Type template, Type awaitableFuncType, Type awaitableActionType, string suffix)
    {
        Template = template;
        Suffix = suffix;
        AwaitableFuncType = awaitableFuncType;
        AwaitableActionType = awaitableActionType;
    }

    /// <summary>
    /// 生成内容を決めるテンプレートをタイプで指定します
    /// </summary>
    public Type Template { get; }
    /// <summary>
    /// Func系コールバックに利用するジェネリック型をタイプで指定します
    /// </summary>
    public Type? AwaitableFuncType { get; }
    /// <summary>
    /// Action系コールバックに利用するジェネリック型をタイプで指定します
    /// </summary>
    public Type? AwaitableActionType { get; }
    /// <summary>
    /// 関数名のサフィックスを指定します
    /// </summary>
    public string? Suffix { get; }
}
