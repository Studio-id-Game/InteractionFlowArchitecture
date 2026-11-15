using System;

namespace InteractionFlow.Core.ResultAPI.Attributes;

/// <summary>
/// 拡張メソッドの生成対象となる Result 型を指定します。
/// 成功判定には <c>IsSuccess</c> を使います。必要な <c>CreateSuccess</c> または <c>CreateFailure</c> の
/// 引数を受け手の同名プロパティから供給できない場合、その候補は生成しません。
/// </summary>
[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class ResultAPIResultAttribute : Attribute
{
}
