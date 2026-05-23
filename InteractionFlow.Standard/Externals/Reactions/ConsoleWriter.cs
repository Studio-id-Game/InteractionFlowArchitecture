using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Externals.Reactions;
using InteractionFlow.Standard.Entities;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.ExternalPorts.ReactionPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.Externals.Reactions
{
    public class ConsoleWriter : Reaction, IConsoleWriter
    {
        public ConsoleWriter() : base()
        {
            if (State == null)
                throw new ArgumentNullException("state");
        }

        public ConsoleState State { get; set; }

        public override void ForceResetMemoryState()
        {
            State = ConsoleState.Default;
        }

        public ValueTask<FlowEndToken> Write(IFlowContext context, ConsoleOutput consoleOutput)
        {
            using var cc = new ConsoleColorScope().GetStateScope();
            cc.State = State.ColorSet;

            if (State.writeLine)
            {
                Console.WriteLine(consoleOutput.text);
            }
            else
            {
                Console.Write(consoleOutput.text);
            }

            return new(CreateFlowEndToken(context));
        }
    }
}
