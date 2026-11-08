using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.SystemFlows;
using InteractionFlow.Samples.Parrot.Entities;
using InteractionFlow.Samples.Parrot.Interactions;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Parrot.SystemFlows
{
    internal sealed class ParrotApplication(SelectSample selectSample, TalkToParrot parrot)
        : SystemFlow<IFlowContext>(selectSample, parrot)
    {
        protected override async Task<InteractionEnd> ExecuteCoreAsync(IFlowContext context)
        {
            while (true)
            {
                var selection = new SampleSelection();
                using var selectionContext = new ScopedFlowContext(context).With(selection);
                var end = await selectSample.ExecuteAsync(selectionContext);
                if (end.HasException || end.HasCanceled || selection.ExitRequested) return end;
                if (selection.Mode == SampleMode.None) continue;

                using var sampleContext = new ScopedFlowContext(context).With(selection.Mode);
                if (selection.Mode == SampleMode.ParrotCustomContext)
                    sampleContext.With(new ParrotGreeting("Hello! I'm Parrot with Custom Context, who are you?"));

                end = await parrot.ExecuteAsync(sampleContext);
                // A handled cancellation ends this conversation, not the application.
                if (end.HasException) return end;
            }
        }
    }
}
