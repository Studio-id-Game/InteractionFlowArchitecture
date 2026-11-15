using InteractionFlow.Core.ResultAPI.Attributes;
using System.Threading.Tasks;

namespace InteractionFlow.Core.ResultAPI;

/// <summary>
/// コードジェネレーターで生成される、ValueTask版のResult拡張メソッドの生成先です
/// </summary>
[ResultAPIExtension(template: typeof(ResultTemplate), awaitableFuncType: typeof(ValueTask<>), awaitableActionType: typeof(ValueTask), suffix: "ValueAsync")]
public static partial class ResultExtensionsValueAsync
{

}
