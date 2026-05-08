using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Reactions;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.ReactionPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.Reactions
{
    public class ConsoleExceptionHandling : ExceptionHandling, IConsoleReaction
    {
        public ConsoleState State { get; set; }

        public override void ForceResetMemoryState()
        {
        }

        protected override ValueTask<FlowEndToken> HandleExceptionCoreAsync(IFlowContext context, Exception exception)
        {
            using (this.GetStateScope())
            {
                Console.ForegroundColor = State.foregroundColor;
                Console.BackgroundColor = State.backgroundColor;

                if (State.writeLine)
                {
                    Console.WriteLine();
                    Console.WriteLine($"* Exception: {exception.GetType().FullName}:");
                    Console.WriteLine($"\t{exception.Message},");
                    Console.WriteLine($"\t{exception.Source};");
                }
                else
                {
                    Console.Write($"* Exception: {exception.GetType().FullName}: {exception.Message}, {exception.Source}; ");
                }
            }

            return default;
        }
    }
}
