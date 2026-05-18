using InteractionFlow.Core.Builders;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Samples.Parrot.Entities;
using InteractionFlow.Samples.Parrot.Focuses;
using InteractionFlow.Samples.Parrot.Interactions;
using InteractionFlow.Samples.Parrot.StoragePorts;
using InteractionFlow.Samples.Parrot.Storages;
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

            // Console Functions
            globalScopeBuilder
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

            using (globalScope)
            {

                var user = new UserObject("InteractionFlow.Sample.Parrot.Main");
                var context = new FlowContext(user);

                using (var initializeApplication = globalScope.BuildFocus<InitializeApplication, IFlowContext>())
                {
                    await initializeApplication.ExecuteAsync(context);
                }

                using var selectAndRunSample = globalScope.BuildFocus<SelectAndRunSample, IFlowContext>();

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
}
