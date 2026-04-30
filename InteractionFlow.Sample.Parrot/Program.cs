using InteractionFlow.Core.Builders;
using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Entities.Rules.Architectures;
using InteractionFlow.Samples.Parrot.Entities;
using InteractionFlow.Samples.Parrot.Focuses;
using InteractionFlow.Samples.Parrot.Interactions;
using InteractionFlow.Samples.Parrot.StoragePorts;
using InteractionFlow.Samples.Parrot.Storages;
using InteractionFlow.Standard.Builders;
using InteractionFlow.Standard.Builders.Profiles;
using InteractionFlow.Standard.Interactions;

namespace InteractionFlow.Samples.Parrot
{
    internal class Program
    {
        private static readonly ScopeHandler globalScope;
        private static readonly FocusHandler<IFlowContext> initializeApplication;
        private static readonly FocusHandler<IFlowContext> selectAndRunSample;

        static Program()
        {
            /*
            // 手動DI

            // Entities
            cancel = new CancellationObject();

            // External Functions as Ports
            var _consoleReaction = new ConsoleReaction();
            IConsoleReaction consoleReaction = _consoleReaction;
            IExceptionPort exceptionPort = _consoleReaction;
            ICancellationPort cancellationPort = _consoleReaction;
            ILastSelectMemory lastSelectMemory = new LastSelectMemory();
            IConsoleOperation consoleOperation = new ConsoleOperation();
            IConsoleOperation.IDummy consoleOperationDummy = new ConsoleOperation.Dummy();

            // Interactions
            var consoleWrite = new ConsoleWrite(exceptionPort, cancellationPort, consoleReaction, default);
            var listSamples = new ListSamples(exceptionPort, cancellationPort, consoleReaction);
            var selectSample = new SelectSample(exceptionPort, cancellationPort, consoleReaction, consoleOperation, lastSelectMemory);
            var runSample = new RunSample(cancel, exceptionPort, cancellationPort, consoleOperation, consoleOperationDummy, consoleReaction, lastSelectMemory);

            // Focuses
            initialize = new InitializeApplication(cancel, consoleWrite);
            select = new SelectAndRunSample(cancel, consoleWrite, listSamples, selectSample, runSample);

            // Focus が static に出来て、Function を共有できるのは、非並列実行環境だから
            */

            var globalScopeBuilder = new ScopeBuilder();

            // Console Reactions & Operations
            globalScopeBuilder.Apply(ConsoleFunction.Profile);

            // Storages
            globalScopeBuilder.Use<ILastSelectMemory, LastSelectMemory>();

            // Interactions
            globalScopeBuilder.Use<ConsoleWrite>();
            globalScopeBuilder.Use<ListSamples>();
            globalScopeBuilder.Use<SelectSample>();
            globalScopeBuilder.Use<RunSample>();

            // Focuses
            globalScope = globalScopeBuilder.BuildScope();
            initializeApplication = globalScope.BuildFocus<InitializeApplication, IFlowContext>();
            selectAndRunSample = globalScope.BuildFocus<SelectAndRunSample, IFlowContext>();
        }

        private static async Task Main(string[] _)
        {
            using (globalScope)
            using (initializeApplication)
            using (initializeApplication)
            {
                var user = new UserObject("InteractionFlow.Sample.Parrot.Main");
                var context = new FlowContext(user);

                var end = await initializeApplication.UseUserFlowAsync(context);

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
