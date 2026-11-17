using InteractionFlow.Core.ResultAPI.Entities.Attributes;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace InteractionFlow.Core.ResultAPI.Entities;

/// <summary>
/// コードジェネレーターで生成される、ValueTask版のResult拡張メソッドの生成先です
/// </summary>
[ResultAPIExtension(template: typeof(ResultTemplate), awaitableFuncType: typeof(ValueTask<>),
    awaitableActionType: typeof(ValueTask), suffix: "ValueAsync", ConfiguredFuncType = typeof(ConfiguredValueTaskAwaitable<>))]
public static partial class ResultExtensionsValueAsync
{

}
