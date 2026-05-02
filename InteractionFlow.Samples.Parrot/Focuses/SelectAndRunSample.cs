using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Focuses;
using InteractionFlow.Core.Interactions;
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
        : Focus<IFlowContext>
    {
        public override IEnumerable<IInteraction> Interactions
        {
            get
            {
                yield return write;
                yield return listSamples;
                yield return selectSample;
                yield return runSample;
            }
        }

        public override async Task<FlowEndToken> FlowWithUserAsync(IFlowContext context)
        {
            context = new FlowContextGroup(context)
                .Add(new ConsoleOutput(), out var textContext)
                .Add(SelectAndRunSampleEndState.None, out var endState);

            await Write("# Select and Run Sample");

            // List
            await listSamples.InteractWithUserAsync(context);

            // Select
            var selectTask = selectSample.InteractWithUserAsync(context);
            context.Cancellation.AddCancelableTask(selectTask);
            var end = await selectTask;
            context = end.LastContext;

            if (await context.Cancellation.TryWaitAndReset())
            {
                await Write("[Exit Sample Select]");
                endState.Value = SelectAndRunSampleEndState.CancelSelect;
                return end;
            }

            // Run
            if (context.TryGet<SampleSelected>(out var selected) && selected.id.mode != SampleMode.None)
            {
                var runTask = runSample.InteractWithUserAsync(context);
                context.Cancellation.AddCancelableTask(runTask);
                end = await runTask;

                if (await context.Cancellation.TryWaitAndReset())
                {
                    await Write("[Exit Sample]");
                    await Write("");
                    endState.Value = SelectAndRunSampleEndState.CancelSample;
                    return end;
                }
                else
                {
                    endState.Value = SelectAndRunSampleEndState.Finish;
                    await Write("[Finish]");
                    return end;
                }
            }


            endState.Value = SelectAndRunSampleEndState.None;
            Console.WriteLine("[None]");
            return end;

            async Task<FlowEndToken> Write(string text)
            {
                textContext.Value = new ConsoleOutput(text);
                var res = await write.InteractWithUserAsync(context);
                await Task.Delay(100);
                return res;
            }

        }
    }
}
