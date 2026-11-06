using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals;
using InteractionFlow.Samples.Parrot.ExternalPorts;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Parrot.Externals
{
    // Uses the same input contract without consuming the menu's console input.
    internal sealed class AutomaticLineInput : Operation, IAutomaticLineInput
    {
        public override void ForceResetMemoryState() { }

        public async ValueTask<string?> ReadLineAsync(IFlowContext context)
        {
            await Task.Delay(500, context.Cancellation.GetToken());
            return "I'm Auto Text to Parrot!";
        }
    }
}
