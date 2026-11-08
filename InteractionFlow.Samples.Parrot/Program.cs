using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Samples.Parrot.ExternalPorts;
using InteractionFlow.Samples.Parrot.Externals;
using InteractionFlow.Samples.Parrot.Interactions;
using InteractionFlow.Samples.Parrot.SystemFlows;
using InteractionFlow.Standard.Builders;
using InteractionFlow.Standard.ConsoleSystem.Builders;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Parrot
{
    internal static class Program
    {
        private static async Task Main()
        {
            var builder = new ScopeBuilder();
            builder.Apply(ConsoleProfile.Default)
                .UseFunction<ILastSelectMemory, LastSelectMemory>()
                .UseFunction<IAutomaticLineInput, AutomaticLineInput>()
                .UseInteraction<SelectSample>()
                .UseInteraction<TalkToParrot>();
            using var scope = builder.BuildScope();
            using var flow = scope.BuildSystemFlow<ParrotApplication, IFlowContext>();
            using var context = new FlowContext();
            var end = await flow.ExecuteAsync(context);
            if (end.HasException) Environment.ExitCode = 1;
        }
    }
}
