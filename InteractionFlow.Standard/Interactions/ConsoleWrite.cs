using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Core.ReactionPorts;
using InteractionFlow.Standard.Entities.Consoles;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.Interactions
{
    public class ConsoleWrite(IExceptionPort exception, ICancellationPort cancellation, IReactionPort<ConsoleOutput> reaction) : Interaction(exception, cancellation)
    {
        private readonly IReactionPort<ConsoleOutput> reaction = reaction;
        private readonly ConsoleOutput reactionValue = new("Default ConsoleWrite Text.");

        public override IEnumerable<IFlowNodePortLayer> Ports
        {
            get
            {
                foreach (var item in base.Ports)
                {
                    yield return item;
                }

                yield return reaction;
            }
        }

        public override async Task<FlowEndToken> InteractWithUserAsync(IFlowContext context)
        {
            try
            {
                if (context.TryGetCanceledException(out var canceledException))
                {
                    return await EndInteractAsync(context, canceledException!);
                }

                if (context.TryGet<ConsoleOutput>(out var consoleOutput))
                {
                    return await EndInteractAsync(context, reaction, consoleOutput);
                }
                else
                {
                    return await EndInteractAsync(context, reaction, reactionValue);
                }
            }
            catch (OperationCanceledException e)
            {
                return await EndInteractAsync(context, e);
            }
            catch (Exception e)
            {
                return await EndInteractAsync(context, e);
            }
        }
    }
}
