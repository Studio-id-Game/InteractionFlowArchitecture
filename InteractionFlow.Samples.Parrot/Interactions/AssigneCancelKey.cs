using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Core.ReactionPorts;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.ReactionPorts;
using InteractionFlow.Standard.SilentlntegrationPorts;

namespace InteractionFlow.Samples.Parrot.Interactions
{
    internal class AssigneCancelKey(IExceptionPort exception, ICancellationPort cancellation, ICancelKeyAssigne cancelKeyAssigne, IConsoleReaction console) : Interaction(exception, cancellation)
    {
        public override async Task<FlowEndToken> InteractWithUserAsync(IFlowContext context)
        {
            await Write("## Assigne CancelKey...");
            await cancelKeyAssigne.IntegrateWithExternalAsync(context, null);
            await Task.Delay(200);
            await Write("CancelKeyPress Assigned.");
            return await Write("");

            async Task<FlowEndToken> Write(string text)
            {
                var output = new ConsoleOutput(text);
                var end = await EndInteractAsync(context, console, output);
                return end;
            }
        }
    }
}
