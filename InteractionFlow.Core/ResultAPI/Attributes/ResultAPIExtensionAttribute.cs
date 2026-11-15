using System;

namespace InteractionFlow.Core.ResultAPI.Attributes;

/// <summary>
/// Result 拡張メソッドの生成先を指定します。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class ResultAPIExtensionAttribute : Attribute
{
    /// <summary>
    /// 同期版の生成先を指定します。
    /// </summary>
    /// <param name="template">生成に使うテンプレート型。</param>
    public ResultAPIExtensionAttribute(Type template)
    {
        Template = template;
    }

    /// <summary>
    /// 非同期版の生成先を指定します。
    /// </summary>
    /// <param name="template">生成に使うテンプレート型。</param>
    /// <param name="awaitableFuncType">結果値を持つ待機可能型。例は <c>Task&lt;&gt;</c> です。</param>
    /// <param name="awaitableActionType">結果値を持たない待機可能型。例は <c>Task</c> です。</param>
    /// <param name="suffix">生成メソッド名の接尾辞。</param>
    public ResultAPIExtensionAttribute(Type template, Type awaitableFuncType, Type awaitableActionType, string suffix)
    {
        Template = template;
        Suffix = suffix;
        AwaitableFuncType = awaitableFuncType;
        AwaitableActionType = awaitableActionType;
    }

    /// <summary>
    /// テンプレート型を取得します。
    /// </summary>
    public Type Template { get; }
    /// <summary>
    /// 結果値を持つ待機可能型を取得します。
    /// </summary>
    public Type? AwaitableFuncType { get; }
    /// <summary>
    /// 結果値を持たない待機可能型を取得します。
    /// </summary>
    public Type? AwaitableActionType { get; }
    /// <summary>
    /// 生成メソッド名の接尾辞を取得します。
    /// </summary>
    public string? Suffix { get; }
}
