using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Entities.Rules.Architectures;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Core.ReactionPorts;
using InteractionFlow.Samples.Parrot.Entities.ParrotContexts;
using InteractionFlow.Samples.Parrot.Entities.SampleContexts;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.OperationPorts;
using InteractionFlow.Standard.ReactionPorts;

namespace InteractionFlow.Samples.Parrot.Interactions
{


    internal class Parrot(
        IExceptionPort exception,
        ICancellationPort cancellation,
        IConsoleOperation operation,
        IConsoleReaction reaction)
        : Interaction(exception, cancellation), IInteraction
    {
        private static string DefaultHello => $"Hello! I'm Parrot, who are you?";

        protected override async ValueTask<FlowEndToken> SystemFlowCoreAsync(IFlowContext context)
        {
            await reaction.ReactToUserAsync(context, new ConsoleOutput($"## Parrot"));

            if (!context.TryGet<SampleSelected>(out var selectedSample))
            {
                await reaction.ReactToUserAsync(context, new ConsoleOutput($"* Sample not selected."));
                return await ReactAndGetEndToken(context, reaction, new ConsoleOutput(""));
            }
            else
            {
                await reaction.ReactToUserAsync(context, new ConsoleOutput($"- {selectedSample}"));
            }

            await Hello(context);

            FlowEndToken? end;

            do
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                try
                {
                    end = await SingleParrot(context);
                }
                catch (OperationCanceledException e)
                {
                    end = await CancellationInteractAsync(context, e);
                }
                catch (Exception e)
                {
                    end = await ExceptionInteractAsync(context, e);
                }

            } while (!end.HasCanceledException);

            return end;
        }

        private async Task Hello(IFlowContext context)
        {
            if (!context.TryGet<ParrotHello>(out var hello) || hello.text == null)
            {
                hello = new ParrotHello(DefaultHello);
            }

            if (!string.IsNullOrEmpty(hello.text))
            {
                await NameHeader(context, "Parrot");
                await SlowTalk(context, hello.text);
            }
        }

        private async Task<FlowEndToken> SingleParrot(IFlowContext context)
        {
            var input = await Input(context);

            if (string.IsNullOrWhiteSpace(input.text))
            {
                throw new ArgumentException("No blank fields allowed.");
            }

            string outputText = GetReactionText(input);

            await Task.Delay(100);

            return await Output(context, outputText);
        }

        private async Task<ConsoleInputText> Input(IFlowContext context)
        {
            await NameHeader(context, "You");

            using var operationState = operation.State.Customize(e => operation.State = e);
            operationState.Set(writeLine: false);
            return await operation.UserOperateTextAsync(context);
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
            using var reactionState = reaction.State.Customize(e => reaction.State = e);
            reactionState.Set(writeLine: false);
            return await ReactAndGetEndToken(context, reaction, new ConsoleOutput($"{name} : "));

        }

        private async Task<FlowEndToken> SlowTalk(IFlowContext context, string outputText)
        {
            using var reactionState = reaction.State.Customize(e => reaction.State = e);

            reactionState.Set(writeLine: false);

            foreach (var item in outputText)
            {

                await reaction.ReactToUserAsync(context, new ConsoleOutput(item.ToString()));
                await Task.Delay(50);
                context.CancellationToken.ThrowIfCancellationRequested();
            }

            reactionState.Reset();

            return await ReactAndGetEndToken(context, reaction, new ConsoleOutput(""));
        }
    }
}
