using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.SystemFlows;
using InteractionFlow.Samples.Raylib.Entities.Contexts;
using InteractionFlow.Samples.Raylib.Interactions;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Raylib.SystemFlows;

public sealed class CounterSystemFlow(IncrementCounter interaction) : SystemFlow<CounterFlowContext>(interaction)
{
    protected override async Task<InteractionEnd> ExecuteCoreAsync(CounterFlowContext context)
    {
        while (true)
        {
            var end = await interaction.ExecuteAsync(context);

            if (end.HasException || context.State.ExitRequested)
                return end;
        }
    }
}
