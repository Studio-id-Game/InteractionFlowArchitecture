using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Core.ReactionPorts;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.ReactionPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.Interactions
{
    public class ConsoleWriting(
        IExceptionPort<Exception> exception,
        ICancellationPort cancellation,
        IConsoleWriter consoleWrite)
        : Interaction(exception, cancellation, consoleWrite)
    {
        private ConsoleOutput DefaultReactionValue => new("Default ConsoleWrite Text.");

        public override async Task<FlowEndToken> InteractWithUserAsync(IFlowContext context)
        {
            return await TryCatchBlock(context, async (context) =>
            {
                var output = context.TryGet<ConsoleOutput>(out var _output) ? _output : DefaultReactionValue;
                return await consoleWrite.Write(context, output);
            });
        }
    }
}
