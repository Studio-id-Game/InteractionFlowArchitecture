using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.SystemFlows;
using InteractionFlow.Samples.Parrot.Interactions;
using InteractionFlow.Standard.Interactions;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Parrot.SystemFlows
{

    internal class InitializeApplication(
        ConsoleWriting writing,
        ConsoleSetup assigneCancelKey)
        : SystemFlow<IFlowContext>(writing, assigneCancelKey)
    {
        protected override async Task<FlowEndToken> ExecuteCoreAsync(IFlowContext context)
        {
            await Write("# Interaction Flow Architecture's Sample : Parrot Application.");
            await Write("- Application Initializing Start...");

            await assigneCancelKey.ExecuteAsync(context);

            await Write("> Application Initializing Complete.");
            return await Write("");

            async Task<FlowEndToken> Write(string text)
            {
                var res = await writing.ExecuteAsync(context, (new(text), null));
                await Task.Delay(50);
                return res;
            }
        }
    }
}
