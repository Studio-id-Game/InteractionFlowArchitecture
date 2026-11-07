using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Samples.Parrot.Entities;
using InteractionFlow.Samples.Parrot.ExternalPorts;
using InteractionFlow.Standard.ConsoleSystem.Entities;
using InteractionFlow.Standard.ConsoleSystem.ExternalPorts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Parrot.Interactions
{
    internal sealed class TalkToParrot(
        IConsoleLineInputPort input,
        IAutomaticLineInput automaticInput,
        IConsoleOutputPort output,
        IConsoleCancellationBindingPort binding,
        IExceptionPort<Exception> exception,
        ICancellationPort cancellation)
        : Interaction(exception, cancellation, input, automaticInput, output, binding)
    {
        protected override async Task<ReactionEnd> ExecuteCoreAsync(IFlowContext context)
        {
            if (!context.TryGet<SampleMode>(out var mode))
                throw new InvalidOperationException("No sample mode context.");

            var automatic = mode is SampleMode.ParrotAuto or SampleMode.ParrotAutoAndKill;
            var style = mode == SampleMode.ParrotColorful ? new ConsoleTextStyle(ConsoleColor.Cyan) : default;
            var greeting = context.TryGet<ParrotGreeting>(out var custom)
                ? custom.Text : ParrotRules.DefaultGreeting;

            using var bindingScope = binding.Bind(context);
            // The timer belongs to this execution and is disposed before cancellation handling resets the context.
            using var timer = new CancellationTokenSource();
            using var registration = timer.Token.Register(context.Cancellation.Cancel);
            if (mode == SampleMode.ParrotAutoAndKill) timer.CancelAfter(TimeSpan.FromSeconds(10));
            var end = await output.WriteLineAsync(context,
                "# Parrot — Ctrl+C returns to the menu. Blank or EOF ends a conversation.", style);
            if (end.HasException) return end;
            end = await SlowTalkAsync(context, "Parrot: " + greeting, style);
            if (end.HasException) return end;

            while (true)
            {
                end = await output.WriteAsync(context, "You: ");
                if (end.HasException) return end;
                var text = await (automatic ? automaticInput : input).ReadLineAsync(context);
                if (string.IsNullOrWhiteSpace(text))
                    return await output.WriteLineAsync(context, "Parrot: See you on the next perch!", style);

                if (automatic)
                {
                    end = await output.WriteLineAsync(context, text);
                    if (end.HasException) return end;
                }
                end = await SlowTalkAsync(context, "Parrot: " + ParrotRules.Reply(text), style);
                if (end.HasException) return end;
            }
        }

        private async Task<ReactionEnd> SlowTalkAsync(IFlowContext context, string text, ConsoleTextStyle style)
        {
            foreach (var character in text)
            {
                context.Cancellation.GetToken().ThrowIfCancellationRequested();
                var end = await output.WriteAsync(context, character.ToString(), style);
                if (end.HasException) return end;
                await Task.Delay(25, context.Cancellation.GetToken());
            }
            return await output.WriteLineAsync(context, "", style);
        }
    }
}
