using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts.ReactionPorts;
using InteractionFlow.Standard.Entities;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.ExternalPorts.ReactionPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.Interactions
{
    public class ConsoleWriting(
        IExceptionPort<Exception> exception,
        ICancellationPort cancellation,
        IConsoleWriter consoleWrite)
        : InteractionOptionalArg<(ConsoleOutput?, ConsoleState?)>(exception, cancellation, consoleWrite)
    {
        protected override (ConsoleOutput?, ConsoleState?) DefaultOption => (DefaultOutput, DefaultState);
        protected virtual ConsoleOutput DefaultOutput => new("Default ConsoleWrite Text.");
        protected virtual ConsoleState DefaultState => ConsoleState.Default;

        public override async Task<FlowEndToken> ExecuteAsync(IFlowContext context, (ConsoleOutput?, ConsoleState?) option)
        {
            return await TryCatchBlock(context, option, InteractWithUserAsyncCore);
        }

        protected virtual async Task<FlowEndToken> InteractWithUserAsyncCore(IFlowContext context, (ConsoleOutput?, ConsoleState?) option)
        {
            var output = option.Item1 ?? (context.TryGet<ConsoleOutput>(out var _output) ? _output! : DefaultOutput);
            var state = option.Item2 ?? (context.TryGet<ConsoleState>(out var _state) ? _state! : DefaultState);

            using var scope = consoleWrite.GetStateScope();
            scope.State = state;
            return await consoleWrite.Write(context, output);
        }
    }
}
