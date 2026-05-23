using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ProgramFlows;
using InteractionFlow.Samples.Parrot.Entities;
using InteractionFlow.Samples.Parrot.Entities.SampleContexts;
using InteractionFlow.Samples.Parrot.Interactions;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.Interactions;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Parrot.ProgramFlows
{
    internal class SelectAndRunSample(
        ConsoleWriting writing,
        ListSamples listSamples,
        SelectSample selectSample,
        RunSample runSample)
        : ProgramFlow<IFlowContext>(writing, listSamples, selectSample, runSample)
    {
        public override async Task<FlowEndToken> ExecuteAsync(IFlowContext context)
        {
            context = new FlowContextGroup(context)
                .Add(SelectAndRunSampleEndState.None, out var endState);

            async Task<FlowEndToken> Write(string text)
            {
                var res = await writing.ExecuteAsync(context, (new ConsoleOutput(text), null));
                await Task.Delay(100);
                return res;
            }

            await Write("# Select and Run Sample (Press Ctrl + C to cancel the selection.)");

            // List
            await listSamples.ExecuteAsync(context);

            // Select
            var end = await selectSample.ExecuteAsync(context);
            context = end.LastContext;

            if (end.HasCanceled)
            {
                await Write("[Exit Sample Select]");
                endState.Value = SelectAndRunSampleEndState.CancelSelect;
                return end;
            }

            // Run
            if (context.TryGet<SampleSelected>(out var selected) && selected.id.mode != SampleMode.None)
            {
                end = await runSample.ExecuteAsync(context);

                if (end.HasCanceled)
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

        }
    }
}
