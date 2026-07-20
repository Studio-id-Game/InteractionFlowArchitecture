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
using System.Threading.Tasks;

namespace InteractionFlow.Samples.HelloDoor
{
    internal static class Program
    {
        private static async Task Main()
        {
            var builder = new ScopeBuilder();

            builder
                .Apply(ConsoleBuilder.Profile)
                .UseFunction<IDoorOperation, ConsoleDoorOperation>()
                .UseFunction<IDoorReaction, ConsoleDoorReaction>()
                .UseInteraction<OperateDoor>();

            using var scope = builder.BuildScope();
            using var flow = scope.BuildSystemFlow<DoorSystemFlow, IFlowContext>();

            using var context = new ScopedFlowContext(new FlowContext())
                .With(new DoorState { IsOpen = false });

            await flow.ExecuteAsync(context);
        }
    }
}
