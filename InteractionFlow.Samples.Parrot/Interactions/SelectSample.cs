using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Samples.Parrot.Entities;
using InteractionFlow.Samples.Parrot.ExternalPorts;
using InteractionFlow.Standard.ConsoleSystem.ExternalPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Parrot.Interactions
{
    internal sealed class SelectSample(
        IConsoleLineInputPort input,
        IConsoleOutputPort output,
        IConsoleCancellationBindingPort binding,
        ILastSelectMemory memory,
        IExceptionPort<Exception> exception,
        ICancellationPort cancellation)
        : Interaction(exception, cancellation, input, output, binding, memory)
    {
        protected override Task OnCancellation(IFlowContext context)
        {
            // Successful cancellation handling returns a successful ReactionEnd, so retain the menu exit intent.
            if (context.TryGet<SampleSelection>(out var selection)) selection.ExitRequested = true;
            return Task.CompletedTask;
        }

        protected override async Task<ReactionEnd> ExecuteCoreAsync(IFlowContext context)
        {
            if (!context.TryGet<SampleSelection>(out var selection))
                throw new InvalidOperationException("No sample selection context.");

            using var bindingScope = binding.Bind(context);
            var end = await output.WriteLineAsync(context,
                "\n# Parrot samples\n[0] Parrot\n[1] ParrotAuto\n[2] ParrotAutoAndKill\n" +
                "[3] ParrotColorful\n[4] ParrotCustomContext\n[5] RepeatLast\n" +
                "Enter a name or index. Blank, EOF or Ctrl+C exits the menu.");
            if (end.HasException) return end;

            var text = await input.ReadLineAsync(context);
            if (string.IsNullOrWhiteSpace(text))
            {
                selection.ExitRequested = true;
                return await output.WriteLineAsync(context, "The parrot waves goodbye!");
            }

            var mode = ParrotRules.ParseSample(text);
            if (mode == SampleMode.None)
                return await output.WriteLineAsync(context, "That perch doesn't exist. Choose a name or index.");

            var last = memory.GetKey(context).Then(memory.GetOrCreate);
            if (!last.Try(out var entry, out var error)) throw error;
            if (mode == SampleMode.RepeatLast)
            {
                mode = entry.Value;
                if (mode == SampleMode.None)
                    return await output.WriteLineAsync(context, "No previous sample yet.");
            }
            else
            {
                entry.Value = mode;
            }

            selection.Mode = mode;
            return await output.WriteLineAsync(context, $"Sample selected: {mode}");

        }
    }
}
