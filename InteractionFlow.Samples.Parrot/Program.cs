using InteractionFlow.Core.Builders;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Samples.Parrot.Entities;
using InteractionFlow.Samples.Parrot.Focuses;
using InteractionFlow.Samples.Parrot.Interactions;
using InteractionFlow.Samples.Parrot.StoragePorts;
using InteractionFlow.Samples.Parrot.Storages;
using InteractionFlow.Standard.Builders;
using InteractionFlow.Standard.Interactions;
using InteractionFlow.Standard.SilentExternalPorts;
using InteractionFlow.Standard.SilentExternals;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Parrot
{
    internal class Program
    {
        private static ScopeHandler BuildScope()
        {
            var globalScopeBuilder = new ScopeBuilder();

            // Console Reactions & Operations
            globalScopeBuilder.Apply(ConsoleBuilder.Profile);

            // Storages
            globalScopeBuilder.UseFunction<ILastSelectMemory, LastSelectMemory>();

            // Silentlntegrations
            globalScopeBuilder.UseFunction<ICancellationWithConsole, CancellationWithConsole>();

            // Interactions
            globalScopeBuilder.UseInteraction<ConsoleWriting>();
            globalScopeBuilder.UseInteraction<ListSamples>();
            globalScopeBuilder.UseInteraction<SelectSample>();
            globalScopeBuilder.UseInteraction<RunSample>();
            globalScopeBuilder.UseInteraction<AssigneCancelKey>();

            return globalScopeBuilder.BuildScope();
        }

        private static async Task Main(string[] _)
        {
            using var globalScope = BuildScope();

            using (globalScope)
            {

                var user = new UserObject("InteractionFlow.Sample.Parrot.Main");
                var context = new FlowContext(user);

                FlowEndToken end;

                {
                    using var initializeApplication = globalScope.BuildFocus<InitializeApplication, IFlowContext>();
                    end = await initializeApplication.UseUserFlowAsync(context);
                }

                using var selectAndRunSample = globalScope.BuildFocus<SelectAndRunSample, IFlowContext>();

                while (true)
                {
                    end = await selectAndRunSample.UseUserFlowAsync(context);
                    var endState = end.LastContext.TryGet<SelectAndRunSampleEndState>(out var _endState) ?
                        _endState : SelectAndRunSampleEndState.None;

                    if (endState == SelectAndRunSampleEndState.CancelSelect)
                        break;
                }
            }
        }
    }
}
