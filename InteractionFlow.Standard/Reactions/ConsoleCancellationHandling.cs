using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Reactions;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.ReactionPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.Reactions
{
    public class ConsoleCancellationHandling : CancellationHandling, IConsoleReaction
    {
        public ConsoleState State { get; set; }

        public override void ForceResetMemoryState()
        {
            State = ConsoleState.Default;
        }

        protected override ValueTask<FlowEndToken> AfterCancellationCoreAsync(IFlowContext context, OperationCanceledException exception)
        {
            using (this.GetStateScope())
            {
                Console.ForegroundColor = State.foregroundColor;
                Console.BackgroundColor = State.backgroundColor;

                if (State.writeLine)
                {
                    Console.WriteLine();
                    Console.WriteLine($"* Cancel: {exception.Message}");
                    Console.WriteLine();
                }
                else
                {
                    Console.Write($"* Cancel: {exception.Message}");
                }
            }

            return default;
        }
    }
}
