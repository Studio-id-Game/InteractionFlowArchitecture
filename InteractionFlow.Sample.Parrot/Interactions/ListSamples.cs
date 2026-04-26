using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Entities.Rules.Architectures;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Core.ReactionPorts;
using InteractionFlow.Samples.Parrot.Entities.SampleContexts;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.ReactionPorts;

namespace InteractionFlow.Samples.Parrot.Interactions
{
    internal class ListSamples(IExceptionPort exception, ICancellationPort cancellation, IConsoleReaction console) : Interaction(exception, cancellation)
    {
        protected override async ValueTask<FlowEndToken> SystemFlowCoreAsync(IFlowContext context)
        {
            var names = Enum.GetNames<SampleMode>().ToList();
            names.Remove(Enum.GetName(SampleMode.None) ?? string.Empty);

            var token = await ReactAndGetEndToken(context, console, new ConsoleOutput("## Samples [index] name"));
            ;
            foreach (var (name, index) in names.Select((e, index) => (e, index)))
            {
                token = await ReactAndGetEndToken(context, console, new ConsoleOutput($"- [{index}] {name}"));
            }

            return token = await ReactAndGetEndToken(context, console, new ConsoleOutput(""));
        }
    }
}