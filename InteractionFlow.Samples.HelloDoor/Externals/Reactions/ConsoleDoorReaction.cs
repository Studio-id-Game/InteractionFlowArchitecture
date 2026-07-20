using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals.Reactions;
using InteractionFlow.Samples.HelloDoor.ExternalPorts.ReactionPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.HelloDoor.Externals.Reactions
{
    internal sealed class ConsoleDoorReaction : Reaction, IDoorReaction
    {
        public override void ForceResetMemoryState()
        {
        }

        public ValueTask<ReactionEnd> WriteAsync(IFlowContext context, string message)
        {
            Console.WriteLine(message);
            return new(GetEnd());
        }
    }
}
