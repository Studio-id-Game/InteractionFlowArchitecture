using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ReactionPorts;
using InteractionFlow.Core.Reactions;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.ReactionPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.Reactions
{
    public class ConsoleReaction : Reaction<ConsoleOutput>, IConsoleReaction, IExceptionPort, ICancellationPort
    {
        FunctionPortTypes IFlowNode.FunctionTypes => FunctionPortTypes.Reaction;

        public ConsoleState State { get; set; } = ConsoleState.Default;

        public ConsoleState ErrorState { get; set; } = ConsoleState.Default;

        public ConsoleState CancelState { get; set; } = ConsoleState.Default;

        public bool ThrowException { get; set; }

        public override ValueTask ReactToUserAsync(IFlowContext context, ConsoleOutput consoleOutput)
        {
            context.Cancellation.GetToken().ThrowIfCancellationRequested();

            using (State.Use())
            {
                Console.Write(consoleOutput.text);
            }

            return default;
        }

        public ValueTask ReactToUserAsync(IFlowContext context, Exception reactionValue)
        {
            if (ThrowException)
            {
                throw reactionValue;
            }

            Console.WriteLine();

            using (ErrorState.Use())
            {
                Console.Write($"* Exception: {reactionValue.GetType().FullName}: {reactionValue.Message}");
            }

            Console.WriteLine();

            return default;
        }

        public ValueTask ReactToUserAsync(IFlowContext context, OperationCanceledException reactionValue)
        {
            if (ThrowException)
            {
                throw reactionValue;
            }

            Console.WriteLine();

            using (CancelState.Use())
            {
                Console.Write($"* Cancel: {reactionValue.Message}");
            }

            Console.WriteLine();

            return default;
        }

        public override void ForceResetMemoryState()
        {
            State = ConsoleState.Default;
            ErrorState = ConsoleState.Default;
            CancelState = ConsoleState.Default;
        }
    }
}
