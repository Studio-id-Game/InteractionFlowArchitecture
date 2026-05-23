using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts.ReactionPorts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Samples.Parrot.Entities.SampleContexts;
using InteractionFlow.Samples.Parrot.ExternalPorts.StoragePorts;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.ExternalPorts.OperationPorts;
using InteractionFlow.Standard.ExternalPorts.ReactionPorts;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Parrot.Interactions
{
    internal class SelectSample(
        IExceptionPort<Exception> exception,
        ICancellationPort cancellation,
        IConsoleWriter reaction,
        IConsoleOperation operation,
        ILastSelectMemory lastSelectMemory)
        : Interaction(exception, cancellation, reaction, operation, lastSelectMemory)
    {
        public override async Task<FlowEndToken> ExecuteAsync(IFlowContext context)
        {
            reaction.State = ConsoleState.Default;
            await reaction.Write(context, new ConsoleOutput("## Select Sample"));

            return await TryCatchBlockAsync(context, async (context) =>
            {
                await reaction.Write(context, new ConsoleOutput("Enter sample name or index : "));

                var input = await operation.WaitUserTextAsync(context);
                var sampleMode = GetSampleMode(input);

                if (sampleMode == SampleMode.None)
                {
                    await reaction.Write(context, new ConsoleOutput("* Not found name and index."));
                }
                else
                {
                    var sampleID = new SampleID(sampleMode);

                    if (sampleMode != SampleMode.RepeatLast)
                    {
                        lastSelectMemory[context] = sampleID;
                    }

                    var selectedSample = new SampleSelected(sampleID);
                    context = new FlowContextGroup(context)
                        .AddImmutable(selectedSample, out _);

                    await reaction.Write(context, new ConsoleOutput($"Sample Selected : '{selectedSample}'."));
                }

                return await reaction.Write(context, new ConsoleOutput($""));
            },
            async () =>
            {
                await Task.Delay(1000);
            });
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
