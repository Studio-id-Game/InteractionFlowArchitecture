using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Core.ReactionPorts;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.ReactionPorts;
using InteractionFlow.Standard.SilentExternalPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Parrot.Interactions
{
    internal class ConsoleSetup(
        IExceptionPort<Exception> exception,
        ICancellationPort cancellation,
        ICancellationWithConsole cancellationWithConsole,
        IConsoleColorAccess consoleColorAccess,
        IConsoleWriter console)
        : Interaction(exception, cancellation, cancellationWithConsole, console)
    {
        public override async Task<FlowEndToken> ExecuteAsync(IFlowContext context)
        {
            //ExceptionPort.ThrowException = true;
            //CancellationPort.ThrowException = true;

            consoleColorAccess.ForceResetMemoryState();

            await Write("## Cancellation Setup...");
            await cancellationWithConsole.Setup(context);
            await Task.Delay(200);
            return await Write("> Cancellation Setup Complete.");

            ValueTask<FlowEndToken> Write(string text)
            {
                var output = new ConsoleOutput(text);
                return console.Write(context, output);
            }
        }
    }
}
