using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Samples.HelloDoor.Entities;
using InteractionFlow.Samples.HelloDoor.ExternalPorts.OperationPorts;
using InteractionFlow.Samples.HelloDoor.ExternalPorts.ReactionPorts;
using InteractionFlow.Samples.HelloDoor.Externals.Operations;
using InteractionFlow.Samples.HelloDoor.Externals.Reactions;
using InteractionFlow.Samples.HelloDoor.Interactions;
using InteractionFlow.Samples.HelloDoor.SystemFlows;
using InteractionFlow.Standard.Builders;
using InteractionFlow.Standard.Console.Builders;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.HelloDoor
{
    internal static class Program
    {
        private static async Task Main()
        {
            var builder = new ScopeBuilder();

            // Port と External 実装、Interaction を DI に登録します。
            builder
                // Interaction の基底クラスが利用する例外/キャンセル表示 Port も登録します。
                .Apply(ConsoleBuilder.Profile)
                .UseFunction<IDoorOperation, ConsoleDoorOperation>()
                .UseFunction<IDoorReaction, ConsoleDoorReaction>()
                .UseInteraction<OperateDoor>();

            using var scope = builder.BuildScope();
            using var flow = scope.BuildSystemFlow<DoorSystemFlow, IFlowContext>();

            using var context = new ScopedFlowContext(new FlowContext())
                .With(new DoorState { IsOpen = false });

            var end = await flow.ExecuteAsync(context);

            if (end.HasException)
            {
                Console.WriteLine(end.Exception);
            }
        }
    }
}
