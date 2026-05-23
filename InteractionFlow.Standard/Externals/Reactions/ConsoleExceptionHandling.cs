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
    public class ConsoleExceptionHandling : ExceptionHandling, IConsoleReaction
    {
        public ConsoleExceptionHandling()
        {
            if (State == null)
                throw new ArgumentNullException("state");
        }

        public ConsoleState State { get; set; }

        public override void ForceResetMemoryState()
        {
            ThrowException = false;
            State = ConsoleState.Default;
            State.Update(foregroundColor: ConsoleColor.Red);
        }

        protected override ValueTask<FlowEndToken> HandleExceptionCoreAsync(IFlowContext context, Exception exception)
        {
            using (var cc = new ConsoleColorScope().GetStateScope())
            {
                cc.State = State.ColorSet;
                if (State.writeLine)
                {
                    Console.WriteLine();
                    Console.WriteLine($"* Exception: {exception.GetType().FullName}:");
                    Console.WriteLine($"\t{exception.Message},");
                    Console.Write($"\t{exception.Source};");
                }
                else
                {
                    Console.Write($"* Exception: {exception.GetType().FullName}: {exception.Message}, {exception.Source}; ");
                }
            }

            if (State.writeLine)
            {
                Console.WriteLine();
            }

            return new(CreateFlowEndToken(context));
        }
    }
}
