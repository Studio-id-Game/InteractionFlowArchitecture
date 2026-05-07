using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Focuses;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Samples.Parrot.Interactions;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.Interactions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Parrot.Focuses
{

    internal class InitializeApplication(ConsoleWrite write, AssigneCancelKey assigneCancelKey) : Focus<IFlowContext>
    {
        private readonly ConsoleWrite write = write;

        public override IEnumerable<IInteraction> Interactions
        {
            get
            {
                yield return write;
            }
        }

        public override async Task<FlowEndToken> FlowWithUserAsync(IFlowContext context)
        {
            var newContext = new FlowContextGroup(context)
                .Add<ConsoleOutput>(default, out var textContext);

            await Write("# Interaction Flow Architecture's Sample : Parrot Application.");
            await Write("");
            await Write("## Application Initializing Start...");
            await Write("");

            await assigneCancelKey.InteractWithUserAsync(context);

            await Write("Application Initializing Complete.");
            return await Write("");

            async Task<FlowEndToken> Write(string text)
            {
                textContext.Value = new ConsoleOutput(text);
                var res = await write.InteractWithUserAsync(newContext);
                await Task.Delay(50);
                return res;
            }
        }
    }
}
