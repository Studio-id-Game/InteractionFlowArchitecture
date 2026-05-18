using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Reactions;
using InteractionFlow.Standard.Entities;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.ReactionPorts;
using InteractionFlow.Standard.UtilityFunctions;
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
            State = ConsoleState.Default.Update(foregroundColor: ConsoleColor.Yellow);
        }

        protected override ValueTask BeforeCancellationCoreAsync(IFlowContext context, OperationCanceledException exception)
        {
            using (var cc = new ConsoleColorScope().GetStateScope())
            {
                cc.State = State.colorSet;
                if (State.writeLine)
                {
                    Console.WriteLine();
                }

                Console.Write($"* Cancel... : {exception.Message} ");
            }

            if (State.writeLine)
            {
                Console.WriteLine();
            }

            return default;
        }

        protected override ValueTask<FlowEndToken> AfterCancellationCoreAsync(IFlowContext context, OperationCanceledException exception)
        {
            using (var cc = new ConsoleColorScope().GetStateScope())
            {
                cc.State = State.colorSet;
                if (State.writeLine)
                {
                    Console.WriteLine();
                }

                Console.Write($"> Cancel Completed.");
            }

            if (State.writeLine)
            {
                Console.WriteLine();
            }

            return new(CreateFlowEndToken(context));
        }
    }
}
