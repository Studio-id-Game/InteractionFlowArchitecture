using InteractionFlow.Core.Entities.Rules.Architectures;
using InteractionFlow.Core.ReactionPorts;
using InteractionFlow.Core.Reactions;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.ReactionPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.Reactions
{
    public class ConsoleReaction : Reaction, IConsoleReaction, IExceptionPort, ICancellationPort
    {
        public ConsoleState State { get; set; } = ConsoleState.Default;

        public ConsoleState ErrorState { get; set; } = ConsoleState.Default;

        public ConsoleState CancelState { get; set; } = ConsoleState.Default;

        public bool ThroughException { get; set; }

        public bool ThroughCancellationException { get; set; }

        public ValueTask ReactToUserAsync(IFlowContext context, ConsoleOutput consoleOutput)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            using (State.Use())
            {
                Console.Write(consoleOutput.text);
            }

            return default;
        }

        public ValueTask ReactToUserAsync(IFlowContext context, Exception reactionValue)
        {
            if (ThroughException)
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
            if (ThroughCancellationException)
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
