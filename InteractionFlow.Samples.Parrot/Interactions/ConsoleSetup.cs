using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Standard.ConsoleSystem.Entities;
using InteractionFlow.Standard.ConsoleSystem.ExternalPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Parrot.Interactions
{
    internal sealed class ConsoleSetup(
        IExceptionPort<Exception> exception,
        ICancellationPort cancellation,
        ICancellationWithConsole cancellationWithConsole,
        IConsoleColorAccess consoleColorAccess,
        IConsoleWriter console)
        : Interaction(exception, cancellation, cancellationWithConsole, consoleColorAccess, console)
    {
        protected override async Task<ReactionEnd> ExecuteCoreAsync(IFlowContext context)
        {
            //ExceptionPort.ThrowException = true;
            //CancellationPort.ThrowException = true;

            consoleColorAccess.ForceResetMemoryState();

            await Write("## Cancellation Setup...");
            await cancellationWithConsole.Setup(context);
            await Task.Delay(200);
            return await Write("> Cancellation Setup Complete.");

            ValueTask<ReactionEnd> Write(string text)
            {
                var output = new ConsoleOutput(text);
                return console.Write(context, output);
            }
        }
    }
}
