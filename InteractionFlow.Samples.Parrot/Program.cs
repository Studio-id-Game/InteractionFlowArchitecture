using InteractionFlow.Core.Builders;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Samples.Parrot.Entities;
using InteractionFlow.Samples.Parrot.ExternalPorts.StoragePorts;
using InteractionFlow.Samples.Parrot.Externals.Storages;
using InteractionFlow.Samples.Parrot.Interactions;
using InteractionFlow.Samples.Parrot.ProgramFlows;
using InteractionFlow.Standard.Builders;
using InteractionFlow.Standard.Interactions;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Parrot
{
    internal class Program
    {
        private static ScopeHandler BuildScope()
        {
            var globalScopeBuilder = new ScopeBuilder();

            globalScopeBuilder
            // Console Functions
                .Apply(ConsoleBuilder.ProfileUseCancellation)
            // Storages
                .UseFunction<ILastSelectMemory, LastSelectMemory>()
            // Interactions
                .UseInteraction<ConsoleWriting>()
                .UseInteraction<ListSamples>()
                .UseInteraction<SelectSample>()
                .UseInteraction<RunSample>()
                .UseInteraction<ConsoleSetup>();

            return globalScopeBuilder.BuildScope();
        }

        private static async Task Main(string[] _)
        {
            using var globalScope = BuildScope();

            var user = new UserObject("InteractionFlow.Sample.Parrot.Main");
            var context = new FlowContext(user);

            using (var initializeApplication = globalScope.BuildProgramFlow<InitializeApplication, IFlowContext>())
            {
                await initializeApplication.ExecuteAsync(context);
            }

            using var selectAndRunSample = globalScope.BuildProgramFlow<SelectAndRunSample, IFlowContext>();

            while (true)
            {
                var end = await selectAndRunSample.ExecuteAsync(context);
                var endState = end.LastContext.TryGet<SelectAndRunSampleEndState>(out var _endState) ?
                    _endState : SelectAndRunSampleEndState.None;

                if (endState == SelectAndRunSampleEndState.CancelSelect)
                    break;
            }
        }
    }
}
