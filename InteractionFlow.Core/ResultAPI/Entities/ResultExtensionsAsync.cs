using InteractionFlow.Core.ResultAPI.Entities.Attributes;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace InteractionFlow.Core.ResultAPI.Entities;

/// <summary>
/// コードジェネレーターで生成される、Task版のResult拡張メソッドの生成先です
/// </summary>
[ResultAPIExtension(template: typeof(ResultTemplate), awaitableFuncType: typeof(Task<>),
    awaitableActionType: typeof(Task), suffix: "Async", ConfiguredFuncType = typeof(ConfiguredTaskAwaitable<>))]
public static partial class ResultExtensionsAsync
{

}
