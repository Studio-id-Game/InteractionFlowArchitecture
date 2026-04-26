using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Entities.Rules.Architectures;
using InteractionFlow.Core.Focuses;
using InteractionFlow.Samples.Parrot.Entities.SampleContexts;
using InteractionFlow.Samples.Parrot.Interactions;
using InteractionFlow.Standard.Entities;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.Interactions;

namespace InteractionFlow.Samples.Parrot.Focuses
{
    internal class SelectAndRunSample(
        CancellationObject cancellationObject,
        ConsoleWrite write,
        ListSamples listSamples,
        SelectSample selectSample,
        RunSample runSample) : Focus<IFlowContext>(write)
    {
        protected override async ValueTask<FlowEndToken> UserFlowCoreAsync(IFlowContext context)
        {
            var newContext = new FlowContextGroup(context)
                .AddMutable<ConsoleOutput>(default, out var textContext);

            await Write("# Select and Run Sample");

            // List
            await listSamples.UseSystemFlowAsync(newContext);

            // Select
            var currentTask = selectSample.UseSystemFlowAsync(newContext).AsTask();
            cancellationObject.CurrentTask = currentTask;
            var end = await currentTask;
            context = end.LastContext;

            if (context.TryGet<SampleSelected>(out var value) && value.id.mode != SampleMode.None)
            {
                currentTask = runSample.UseSystemFlowAsync(context).AsTask();
                // Run
                cancellationObject.CurrentTask = currentTask;
                return await currentTask;
            }
            else
            {
                return end;
            }

            async Task<FlowEndToken> Write(string text)
            {
                textContext.Set(new ConsoleOutput(text));
                var res = await write.UseSystemFlowAsync(newContext);
                await Task.Delay(100);
                return res;
            }

        }
    }
}
