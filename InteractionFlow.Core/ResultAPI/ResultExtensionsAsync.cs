using InteractionFlow.Core.ResultAPI.Attributes;
using System.Threading.Tasks;

namespace InteractionFlow.Core.ResultAPI;

/// <summary>
/// コードジェネレーターで生成される、Task版のResult拡張メソッドの生成先です
/// </summary>
[ResultAPIExtension(template: typeof(ResultTemplate), awaitableFuncType: typeof(Task<>), awaitableActionType: typeof(Task), suffix: "Async")]
public static partial class ResultExtensionsAsync
{

}
