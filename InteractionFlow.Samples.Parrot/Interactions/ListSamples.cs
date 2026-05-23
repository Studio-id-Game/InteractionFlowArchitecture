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
        public override async Task<FlowEndToken> ExecuteAsync(IFlowContext context)
        {
            var token = await console.Write(context, new ConsoleOutput("## Samples [index] name"));

            return await TryCatchBlockAsync(context, async (context) =>
            {
                var names = Enum.GetNames<SampleMode>().ToList();
                names.Remove(Enum.GetName(SampleMode.None) ?? string.Empty);

                foreach (var (name, index) in names.Select((e, index) => (e, index)))
                {
                    token = await console.Write(context, new ConsoleOutput($"- [{index}] {name}"));
                }

                return token = await console.Write(context, new ConsoleOutput(""));
            });
        }
    }
}
