using System;

namespace InteractionFlow.Core.ResultAPI.Entities.Attributes;

/// <summary>
/// 拡張メソッドの生成に使うテンプレートメソッドを指定します。
/// 非同期版は、戻り値を生成先で指定した待機可能型で包みます。
/// </summary>
/// <param name="asyncSelf">受け手を待機する版を生成するかどうか。</param>
/// <param name="asyncCallback">コールバックを待機する版を生成するかどうか。両方が有効なら組み合わせも生成します。</param>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class ResultAPIFuncAttribute(bool asyncSelf = true, bool asyncCallback = true) : Attribute
{
    /// <summary>
    /// 受け手を待機する版を生成するかどうかを取得します。
    /// </summary>
    public bool AsyncSelf { get; } = asyncSelf;
    /// <summary>
    /// コールバックを待機する版を生成するかどうかを取得します。
    /// </summary>
    public bool AsyncCallback { get; } = asyncCallback;
}
