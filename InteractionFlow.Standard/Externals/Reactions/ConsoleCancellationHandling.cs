using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals.Reactions;
using InteractionFlow.Standard.Entities;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.ExternalPorts.ReactionPorts;
using InteractionFlow.Standard.FunctionUtilities;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.Externals.Reactions
{
    public class ConsoleCancellationHandling : CancellationHandling, IConsoleReaction
    {
        public ConsoleCancellationHandling() : base()
        {
            if (State == null)
                throw new ArgumentNullException("state");
        }

        public ConsoleState State { get; set; }

        public override void ForceResetMemoryState()
        {
            ThrowException = false;
            State = ConsoleState.Default;
            State.Update(foregroundColor: ConsoleColor.Yellow);
        }

        protected override ValueTask BeforeCancellationCoreAsync(IFlowContext context, OperationCanceledException exception)
        {
            using (var cc = new ConsoleColorScope().GetStateScope())
            {
                cc.State = State.ColorSet;
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
                cc.State = State.ColorSet;
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
