using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Entities.Rules.Architectures;
using InteractionFlow.Core.Focuses;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.Interactions;

namespace InteractionFlow.Samples.Parrot.Focuses
{

    internal class InitializeApplication(ConsoleWrite write) : Focus<IFlowContext>(write)
    {
        private readonly ConsoleWrite write = write;

        protected override async ValueTask<FlowEndToken> UserFlowCoreAsync(IFlowContext context)
        {
            var newContext = new FlowContextGroup(context)
                .AddMutable<ConsoleOutput>(default, out var textContext);

            await Write("# Interaction Flow Architecture's Sample : Parrot Application.");
            await Write("");
            await Write("## Application Initializing Start...");
            await Write("");
            await Write("- Assigne Console.CancelKeyPress...");
            Console.CancelKeyPress += CancelKeyPress;
            await Write("Console.CancelKeyPress Assigned.");
            await Write("");

            await Write("Application Initializing Complete.");
            return await Write("");

            async Task<FlowEndToken> Write(string text)
            {
                textContext.Set(new ConsoleOutput(text));
                var res = await write.UseSystemFlowAsync(newContext);
                await Task.Delay(50);
                return res;
            }

            void CancelKeyPress(object? sender, ConsoleCancelEventArgs args)
            {
                if (context.Cancellation.HasTask)
                {
                    context.Cancellation.Cancel();
                }
                args.Cancel = true;
            }
        }
    }
}
