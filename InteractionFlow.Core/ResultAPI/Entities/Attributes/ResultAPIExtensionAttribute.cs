using System;

namespace InteractionFlow.Core.ResultAPI.Entities.Attributes;

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
    /// <param name="awaitableFuncType">待機時に型引数の値を返す、未構築の非同期戻り値型。</param>
    /// <param name="awaitableActionType">待機時に値を返さない非同期戻り値型。</param>
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
    /// 結果値を持つ非同期戻り値型を取得します。
    /// </summary>
    public Type? AwaitableFuncType { get; }
    /// <summary>
    /// 結果値を持たない非同期戻り値型を取得します。
    /// </summary>
    public Type? AwaitableActionType { get; }
    /// <summary>
    /// 直接 await する受け手の未構築の型を指定します。
    /// </summary>
    public Type? ConfiguredFuncType { get; set; }
    /// <summary>
    /// 生成メソッド名の接尾辞を取得します。
    /// </summary>
    public string? Suffix { get; }
}
