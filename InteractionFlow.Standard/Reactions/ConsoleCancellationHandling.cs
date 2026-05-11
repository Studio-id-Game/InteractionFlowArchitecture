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
            ThrowException = false;
            var state = ConsoleState.Default;
            State = state.Update(foregroundColor: ConsoleColor.Yellow);
        }

        public void OnStateApply()
        {
            Console.ForegroundColor = State.foregroundColor;
            Console.BackgroundColor = State.backgroundColor;
        }

        protected override ValueTask BeforeCancellationCoreAsync(IFlowContext context, OperationCanceledException exception)
        {
            using (this.GetStateScope(true))
            {
                if (State.writeLine)
                {
                    Console.WriteLine();
                    Console.WriteLine($"* Cancel... : {exception.Message}");
                }
                else
                {
                    Console.Write($"* Cancel... : {exception.Message} ");
                }
            }

            return default;
        }

        protected override ValueTask<FlowEndToken> AfterCancellationCoreAsync(IFlowContext context, OperationCanceledException exception)
        {
            using (this.GetStateScope(true))
            {
                if (State.writeLine)
                {
                    Console.WriteLine($"> Cancel Completed.");
                    Console.WriteLine();
                }
                else
                {
                    Console.Write($"> Cancel Completed.");
                }
            }

            return new(CreateFlowEndToken(context));
        }
    }
}
