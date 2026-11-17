using System;

namespace InteractionFlow.Core.ResultAPI.Entities.Attributes;

/// <summary>
/// ResultAPIのコードジェネレータで利用するために、テンプレートをマークします。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class ResultAPITemplateAttribute : Attribute
{
}
