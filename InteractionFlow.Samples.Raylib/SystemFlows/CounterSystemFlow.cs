using InteractionFlow.Samples.Raylib.Entities.Contexts;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.SystemFlows;
using InteractionFlow.Samples.Raylib.Entities;
using InteractionFlow.Samples.Raylib.Interactions;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Raylib.SystemFlows;

public sealed class CounterSystemFlow(IncrementCounter interaction) : SystemFlow<IFlowContext>(interaction)
{
    protected override async Task<InteractionEnd> ExecuteCoreAsync(IFlowContext context)
    {
        while (true)
        {
            var end = await interaction.ExecuteAsync(context);

            if (end.HasException || context.GetRequired<ICounterStateAccess>().ExitRequested)
                return end;
        }
    }
}
