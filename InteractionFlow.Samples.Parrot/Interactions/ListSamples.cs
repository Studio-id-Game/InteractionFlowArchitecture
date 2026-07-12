using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts.ReactionPorts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Samples.Parrot.Entities.SampleContexts;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.ExternalPorts.ReactionPorts;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Parrot.Interactions
{
    internal class ListSamples(
        IExceptionPort<Exception> exception,
        ICancellationPort cancellation,
        IConsoleWriter console)
        : Interaction(exception, cancellation, console)
    {
        protected override async Task<ReactionEnd> ExecuteCoreAsync(IFlowContext context)
        {
            var end = await console.Write(context, new ConsoleOutput("## Samples [index] name"));

            var names = Enum.GetNames<SampleMode>().ToList();
            names.Remove(Enum.GetName(SampleMode.None) ?? string.Empty);

            foreach (var (name, index) in names.Select((e, index) => (e, index)))
            {
                end = await console.Write(context, new ConsoleOutput($"- [{index}] {name}"));
            }

            return await console.Write(context, new ConsoleOutput(""));
        }
    }
}
