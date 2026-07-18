using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts.ReactionPorts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Samples.Parrot.Entities;
using InteractionFlow.Samples.Parrot.Entities.ParrotContexts;
using InteractionFlow.Samples.Parrot.Entities.SampleContexts;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.ExternalPorts.OperationPorts;
using InteractionFlow.Standard.ExternalPorts.ReactionPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Parrot.Interactions
{


    internal sealed class Parrot(
        IExceptionPort<Exception> exception,
        ICancellationPort cancellation,
        IConsoleOperation operation,
        IConsoleWriter reaction)
        : Interaction(exception, cancellation, operation, reaction)
    {
        private static string DefaultHello => $"Hello! I'm Parrot, who are you?";

        protected override async Task<ReactionEnd> ExecuteCoreAsync(IFlowContext context)
        {
            await reaction.Write(context, new ConsoleOutput($"## Parrot"));

            var end = await Init(context);

            if (end.HasException) return end;

            do
            {
                end = await TrySingleParrotAsync(context);

            } while (!end.HasCanceled);

            return end;
        }

        protected override async Task OnCancellation(IFlowContext context)
        {
            await Task.Delay(500);
        }

        private async Task<ReactionEnd> Init(IFlowContext context)
        {
            if (!context.TryGet<RefEntity<SampleSelected>>(out var selectedSample))
            {
                await reaction.Write(context, new ConsoleOutput($"* Sample not selected."));
                return await reaction.Write(context, new ConsoleOutput(""));
            }
            else
            {
                var end = await reaction.Write(context, new ConsoleOutput($"- {selectedSample.Value}"));

                var helloText = context.TryGet<RefEntity<ParrotHello>>(out var hello) && hello.Value.text != null
                    ? hello.Value.text
                    : DefaultHello;

                if (!string.IsNullOrEmpty(helloText))
                {
                    await NameHeader(context, "Parrot");
                    end = await SlowTalk(context, helloText);
                }

                return end;
            }

        }

        private async Task<ReactionEnd> SingleParrot(IFlowContext context)
        {
            var input = await Input(context);

            if (string.IsNullOrWhiteSpace(input.text))
            {
                throw new InvalidOperationException("No blank fields allowed.");
            }

            string outputText = GetReactionText(input);

            await Task.Delay(100);

            return await Output(context, outputText);
        }

        private async Task<ReactionEnd> TrySingleParrotAsync(IFlowContext context)
        {
            try
            {
                return await SingleParrot(context);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                return await ExceptionPort.HandleExceptionAsync(context, e);
            }
        }

        private async Task<ConsoleInputText> Input(IFlowContext context)
        {
            await NameHeader(context, "You");
            return await operation.WaitUserTextAsync(context);
        }

        private static string GetReactionText(ConsoleInputText input)
        {
            var outputText = input.text;
            if (outputText.Contains("Parrot", StringComparison.Ordinal))
            {
                outputText = $"{outputText} {outputText} {outputText} {outputText} {outputText} {outputText}!!!!!!!!!!";
            }
            else
            {
                outputText = $"{outputText} {outputText} {outputText}!";
            }

            return outputText;
        }

        private async Task<ReactionEnd> Output(IFlowContext context, string reactionText)
        {
            await NameHeader(context, "Parrot");
            return await SlowTalk(context, reactionText);
        }

        private async Task<ReactionEnd> NameHeader(IFlowContext context, string name)
        {
            using var reactionState = reaction.GetStateScope();
            reaction.State.Update(writeLine: false);

            return await reaction.Write(context, new ConsoleOutput($"{name} : "));

        }

        private async Task<ReactionEnd> SlowTalk(IFlowContext context, string outputText)
        {
            using var reactionState = reaction.GetStateScope();
            reaction.State.Update(writeLine: false);

            foreach (var item in outputText)
            {
                await reaction.Write(context, new ConsoleOutput(item.ToString()));
                await Task.Delay(50);
                context.Cancellation.GetToken().ThrowIfCancellationRequested();
            }

            reaction.State.Update(writeLine: true);

            return await reaction.Write(context, new ConsoleOutput(""));
        }
    }
}
