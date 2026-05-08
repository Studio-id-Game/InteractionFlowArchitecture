using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Reactions;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.ReactionPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.Reactions
{
    public class ConsoleWriter : Reaction, IConsoleWriter
    {
        public ConsoleState State { get; set; }

        public override void ForceResetMemoryState()
        {
            State = ConsoleState.Default;
        }

        public void OnStateApply()
        {
            Console.ForegroundColor = State.foregroundColor;
            Console.BackgroundColor = State.backgroundColor;
        }

        public ValueTask<FlowEndToken> Write(IFlowContext context, ConsoleOutput consoleOutput)
        {
            using (this.GetStateScope(true))
            {
                if (State.writeLine)
                {
                    Console.WriteLine(consoleOutput.text);
                }
                else
                {
                    Console.Write(consoleOutput.text);
                }
            }

            return new(CreateFlowEndToken(context));
        }
    }
}
