using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Focuses;
using InteractionFlow.Samples.Parrot.Interactions;
using InteractionFlow.Standard.Interactions;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Parrot.Focuses
{

    internal class InitializeApplication(
        ConsoleWriting writing,
        ConsoleSetup assigneCancelKey)
        : Focus<IFlowContext>(writing, assigneCancelKey)
    {
        public override async Task<FlowEndToken> ExecuteAsync(IFlowContext context)
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
