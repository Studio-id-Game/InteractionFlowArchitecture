using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Core.ReactionPorts;
using InteractionFlow.Samples.Parrot.Entities.ParrotContexts;
using InteractionFlow.Samples.Parrot.Entities.SampleContexts;
using InteractionFlow.Standard.Entities;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.OperationPorts;
using InteractionFlow.Standard.ReactionPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Parrot.Interactions
{


    internal class Parrot(
        IExceptionPort<Exception> exception,
        ICancellationPort cancellation,
        IConsoleOperation operation,
        IConsoleWriter reaction)
        : Interaction(exception, cancellation, operation, reaction)
    {
        private static string DefaultHello => $"Hello! I'm Parrot, who are you?";

        public override async Task<FlowEndToken> ExecuteAsync(IFlowContext context)
        {
            await reaction.Write(context, new ConsoleOutput($"## Parrot"));

            var end = await TryCatchBlockAsync(context, Init);

            if (end.HasException) return end;

            do
            {
                end = await TryCatchBlockAsync(context, SingleParrot, async () =>
                {
                    await Task.Delay(500);
                });

            } while (!end.HasCanceled);

            return end;
        }

        private async Task<FlowEndToken> Init(IFlowContext context)
        {
            if (!context.TryGet<SampleSelected>(out var selectedSample))
            {
                await reaction.Write(context, new ConsoleOutput($"* Sample not selected."));
                return await reaction.Write(context, new ConsoleOutput(""));
            }
            else
            {
                var end = await reaction.Write(context, new ConsoleOutput($"- {selectedSample}"));

                if (!context.TryGet<ParrotHello>(out var hello) || hello.text == null)
                {
                    hello = new ParrotHello(DefaultHello);
                }

                if (!string.IsNullOrEmpty(hello.text))
                {
                    await NameHeader(context, "Parrot");
                    end = await SlowTalk(context, hello.text);
                }

                return end;
            }

        }

        private async Task<FlowEndToken> SingleParrot(IFlowContext context)
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

        private async Task<FlowEndToken> Output(IFlowContext context, string reactionText)
        {
            await NameHeader(context, "Parrot");
            return await SlowTalk(context, reactionText);
        }

        private async Task<FlowEndToken> NameHeader(IFlowContext context, string name)
        {
            using var reactionState = reaction.GetStateScope();
            reaction.State = reaction.State.Update(writeLine: false);
            return await reaction.Write(context, new ConsoleOutput($"{name} : "));

        }

        private async Task<FlowEndToken> SlowTalk(IFlowContext context, string outputText)
        {
            using var reactionState = reaction.GetStateScope();

            reaction.State = reaction.State.Update(writeLine: false);

            foreach (var item in outputText)
            {
                await reaction.Write(context, new ConsoleOutput(item.ToString()));
                await Task.Delay(50);
                context.Cancellation.GetToken().ThrowIfCancellationRequested();
            }

            reaction.State = reaction.State.Update(writeLine: true);

            return await reaction.Write(context, new ConsoleOutput(""));
        }
    }
}
