using System;

namespace InteractionFlow.Core.ResultAPI.Attributes;

/// <summary>
/// ResultAPIのコードジェネレータで利用するために、対象の Result 型をマークします。
/// この中で、<see cref="ResultAPICallbackArgumentAttribute"/>がついたプロパティはコールバック解決に用いられます。
/// また、その型で定義される以下のメンバーは、 <see cref="ResultAPIFuncAttribute"/> でマークされたテンプレート関数の展開において特別な意味を持ちます。
/// <code>
/// public bool IsSuccess { get; } // Self.IsSuccess の置き換えターゲット
/// public static {自身の型} CreateSuccess({任意の引数列}) // Other.CreateSuccess(self) の置き換えターゲット
/// public static {自身の型} CreateFailure({任意の引数列}) // Other.CreateFailure(self) の置き換えターゲット
///
/// [ResultAPICallbackArgument(onSuccess: true)] が指定されたプロパティ // 成功コールバックの引数 及び CreateSuccess の引数置き換えターゲット
/// [ResultAPICallbackArgument(onSuccess: false)] が指定されたプロパティ // 失敗時コールバックの引数 及び CreateFailure の引数置き換えターゲット
/// </code>
/// </summary>
[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class ResultAPIResultAttribute : Attribute
{
}
