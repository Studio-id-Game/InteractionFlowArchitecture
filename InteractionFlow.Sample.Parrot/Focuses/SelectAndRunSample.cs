using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Entities.Rules.Architectures;
using InteractionFlow.Core.Focuses;
using InteractionFlow.Samples.Parrot.Entities;
using InteractionFlow.Samples.Parrot.Entities.SampleContexts;
using InteractionFlow.Samples.Parrot.Interactions;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.Interactions;

namespace InteractionFlow.Samples.Parrot.Focuses
{
    internal class SelectAndRunSample(
        ConsoleWrite write,
        ListSamples listSamples,
        SelectSample selectSample,
        RunSample runSample)
        : Focus<IFlowContext>(write)
    {
        protected override async ValueTask<FlowEndToken> UserFlowCoreAsync(IFlowContext context)
        {
            //TODO : Cancelで中断した場合に、contextが更新されないため、予期せぬ不具合（endStateが無効）が発生
            context = new FlowContextGroup(context)
                .AddMutable(new ConsoleOutput(), out var textContext)
                .AddMutable(SelectAndRunSampleEndState.None, out var endState);

            await Write("# Select and Run Sample");

            // List
            await listSamples.UseSystemFlowAsync(context);

            // Select
            var selectTask = selectSample.UseSystemFlowAsync(context).AsTask();
            context.Cancellation.AddCancelableTask(selectTask);
            var end = await selectTask;
            context = end.LastContext;

            if (await context.Cancellation.TryWaitAndReset())
            {
                await Write("[Exit Sample Select]");
                endState.Set(SelectAndRunSampleEndState.CancelSelect);
                return end;
            }

            // Run
            if (context.TryGet<SampleSelected>(out var selected) && selected.id.mode != SampleMode.None)
            {
                var runTask = runSample.UseSystemFlowAsync(context).AsTask();
                context.Cancellation.AddCancelableTask(runTask);
                end = await runTask;

                if (await context.Cancellation.TryWaitAndReset())
                {
                    await Write("[Exit Sample]");
                    await Write("");
                    endState.Set(SelectAndRunSampleEndState.CancelSample);
                    return end;
                }
                else
                {
                    endState.Set(SelectAndRunSampleEndState.Finish);
                    await Write("[Finish]");
                    return end;
                }
            }


            endState.Set(SelectAndRunSampleEndState.None);
            Console.WriteLine("[None]");
            return end;

            async Task<FlowEndToken> Write(string text)
            {
                textContext.Set(new ConsoleOutput(text));
                var res = await write.UseSystemFlowAsync(context);
                await Task.Delay(100);
                return res;
            }

        }
    }
}
