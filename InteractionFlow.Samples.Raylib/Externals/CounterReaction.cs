using InteractionFlow.Samples.Raylib.Entities.Contexts;
using InteractionFlow.Samples.Raylib.ExternalPorts;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals;
using InteractionFlow.Samples.Raylib.Entities;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Raylib.Externals;

internal sealed class CounterReaction : Reaction, ICounterReaction
{
    public CounterReaction()
    {
    }

    public override void ForceResetMemoryState()
    {
    }

    public async ValueTask<ReactionEnd> RespondAsync(IFlowContext context, CounterCommand command)
    {
        var state = context.GetRequired<ICounterStateAccess>();

        if (command == CounterCommand.Exit)
        {
            state.RequestExit();
        }
        else
        {
            if (!state.BeginIncrement())
                return GetEnd();

            await Task.Delay(1000, context.Cancellation.GetToken());

            state.CompleteIncrement();
        }

        return GetEnd();
    }
}
