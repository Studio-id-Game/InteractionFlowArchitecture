using System;

namespace InteractionFlow.Core.ResultAPI.Attributes;

/// <summary>
/// ResultAPIのコードジェネレータで利用するために、生成するテンプレート関数をマークします。
/// <see cref="ResultAPITemplateAttribute"/> でマークされたテンプレートクラス内でのみ有効です。 
/// </summary>
/// <param name="asyncSelf"> 非同期スタートのオーバーロードを追加生成します。引数の Self を <see cref="ResultAPIExtensionAttribute"/> で指定された非同期クラスで包み、関数の最初に await して利用します。</param>
/// <param name="asyncCallback"> 非同期コールバックのオーバーロードを追加生成します。 <paramref name="asyncSelf"/> との組み合わせも生成します。関数を async で定義し、コールバックと戻り値の Other を <see cref="ResultAPIExtensionAttribute"/> で指定された非同期クラスで包み、コールバック呼び出しは await します。</param>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class ResultAPIFuncAttribute(bool asyncSelf = true, bool asyncCallback = true) : Attribute
{
    /// <summary>
    /// 非同期スタートのオーバーロードを追加生成します。引数の Self を <see cref="ResultAPIExtensionAttribute"/> で指定された非同期クラスで包み、関数の最初に await して利用します。
    /// </summary>
    public bool AsyncSelf { get; } = asyncSelf;
    /// <summary>
    /// 非同期コールバックのオーバーロードを追加生成します。 <see cref="AsyncSelf"/> との組み合わせも生成します。関数を async で定義し、コールバックと戻り値の Other を <see cref="ResultAPIExtensionAttribute"/> で指定された非同期クラスで包み、コールバック呼び出しは await します。
    /// </summary>
    public bool AsyncCallback { get; } = asyncCallback;
}
