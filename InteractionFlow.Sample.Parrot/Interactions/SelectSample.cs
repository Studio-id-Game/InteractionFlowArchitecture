using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Entities.Rules.Architectures;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Core.ReactionPorts;
using InteractionFlow.Samples.Parrot.Entities.SampleContexts;
using InteractionFlow.Samples.Parrot.StoragePorts;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.OperationPorts;
using InteractionFlow.Standard.ReactionPorts;

namespace InteractionFlow.Samples.Parrot.Interactions
{
    internal class SelectSample(
        IExceptionPort exception,
        ICancellationPort cancellation,
        IConsoleReaction reaction,
        IConsoleOperation operation,
        ILastSelectMemory lastSelectMemory)
        : Interaction(exception, cancellation)
    {
        protected override async ValueTask<FlowEndToken> SystemFlowCoreAsync(IFlowContext context)
        {
            await ReactAndGetEndToken(context, reaction, new ConsoleOutput("## Select Sample"));
            await ReactAndGetEndToken(context, reaction, new ConsoleOutput("Enter sample name or index : "));

            var input = await operation.UserOperateTextAsync(context);
            var sampleMode = GetSampleMode(input);

            await ReactAndGetEndToken(context, reaction, new ConsoleOutput(""));

            if (sampleMode == SampleMode.None)
            {
                await ReactAndGetEndToken(context, reaction, new ConsoleOutput("* Not found name and index."));
            }
            else
            {
                var sampleID = new SampleID(sampleMode);

                if (sampleMode != SampleMode.RepeatLast)
                {
                    lastSelectMemory[context] = sampleID;
                }

                var selectedSample = new SampleSelected(sampleID);
                context = new FlowContextGroup(context).Add(selectedSample, out _);

                await ReactAndGetEndToken(context, reaction, new ConsoleOutput($"Sample Selected : '{selectedSample}'."));
            }

            return await ReactAndGetEndToken(context, reaction, new ConsoleOutput($""));
        }

        private static SampleMode GetSampleMode(ConsoleInputText input)
        {
            var text = input.text;

            var sample = SampleMode.None;
            if (int.TryParse(text, out int index))
            {
                var samples = Enum.GetValues<SampleMode>().ToList();
                samples.Remove(SampleMode.None);
                if (0 <= index && index < samples.Count)
                {
                    sample = samples[index];
                }
            }
            else if (!Enum.TryParse(text, true, out sample))
            {
                sample = SampleMode.None;
            }

            return sample;
        }
    }
}
