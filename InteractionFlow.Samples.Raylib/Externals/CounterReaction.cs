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
        var state = context.GetRequired<CounterState>();

        if (command == CounterCommand.Exit)
        {
            state.ExitRequested = true;
            context.GetRequired<CounterDisplayState>().Show(state.Count, "Goodbye");
        }
        else
        {
            context.GetRequired<CounterDisplayState>().Show(state.Count, "Working... (Cancel is available)");
            await Task.Delay(1000, context.Cancellation.GetToken());

            state.Count++;
            context.GetRequired<CounterDisplayState>().Show(state.Count, "Completed");
        }

        return GetEnd();
    }
}
