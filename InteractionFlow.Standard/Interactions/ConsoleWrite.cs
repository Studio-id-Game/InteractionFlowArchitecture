using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Entities.Rules.Architectures;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Core.ReactionPorts;
using InteractionFlow.Standard.Entities.Consoles;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.Interactions
{
    public class ConsoleWrite : Interaction
    {
        private readonly IReactionPort<ConsoleOutput> reaction;
        private readonly ConsoleOutput reactionValue;

        public ConsoleWrite(IExceptionPort exception, ICancellationPort cancellation, IReactionPort<ConsoleOutput> reaction, ConsoleOutput reactionValue)
            : base(exception, cancellation)
        {
            this.reaction = reaction;
            this.reactionValue = reactionValue;
        }

        protected override ValueTask<FlowEndToken> SystemFlowCoreAsync(IFlowContext context)
        {
            if (context.TryGet<ConsoleOutput>(out var consoleOutput))
            {
                return ReactAndGetEndToken(context, reaction, consoleOutput);
            }
            else
            {
                return ReactAndGetEndToken(context, reaction, reactionValue);
            }
        }
    }
}