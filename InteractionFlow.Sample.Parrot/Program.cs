using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ReactionPorts;
using InteractionFlow.Samples.Parrot.Focuses;
using InteractionFlow.Samples.Parrot.Interactions;
using InteractionFlow.Samples.Parrot.StoragePorts;
using InteractionFlow.Samples.Parrot.Storages;
using InteractionFlow.Standard.Entities;
using InteractionFlow.Standard.Interactions;
using InteractionFlow.Standard.OperationPorts;
using InteractionFlow.Standard.Operations;
using InteractionFlow.Standard.ReactionPorts;
using InteractionFlow.Standard.Reactions;

namespace InteractionFlow.Samples.Parrot
{
    internal class Program
    {
        private static readonly CancellationObject cancel;
        private static readonly InitializeApplication initialize;
        private static readonly SelectAndRunSample select;

        static Program()
        {
            // 手動DI

            // Entities
            cancel = new CancellationObject();

            // External Functions as Ports
            var _consoleReaction = new ConsoleReaction() { /*ThroughException = true, ThroughCancellationException = true*/ };
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
        }

        private static async Task Main(string[] _)
        {
            var user = new UserToken("InteractionFlow.Sample.Parrot.Main");
            var context = new FlowContext(user, cancel.GetToken());

            await initialize.UseUserFlowAsync(context);
            while (true)
            {
                context = new FlowContext(user, cancel.GetToken());
                await select.UseUserFlowAsync(context);
            }
        }
    }
}
