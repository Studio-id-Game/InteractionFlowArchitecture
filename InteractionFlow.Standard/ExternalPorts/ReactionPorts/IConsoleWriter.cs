using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Standard.Entities.Consoles;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.ExternalPorts.ReactionPorts
{
    public interface IConsoleWriter : IConsoleReaction
    {
        public ValueTask<FlowEndToken> Write(IFlowContext context, ConsoleOutput consoleOutput);
    }
}
