using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Core.ReactionPorts;
using InteractionFlow.Samples.Parrot.Entities.ParrotContexts;
using InteractionFlow.Samples.Parrot.Entities.SampleContexts;
using InteractionFlow.Samples.Parrot.StoragePorts;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.OperationPorts;
using InteractionFlow.Standard.ReactionPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Parrot.Interactions
{
    internal class RunSample(
        IExceptionPort exception,
        ICancellationPort cancellation,
        IConsoleOperation operation,
        IConsoleOperation.IDummy valueOperation,
        IConsoleReaction reaction,
        ILastSelectMemory lastSelectMemory)
        : Interaction(exception, cancellation), IInteraction
    {
        private readonly Parrot parrot = new(exception, cancellation, operation, reaction);
        private readonly Parrot parrotAuto = new(exception, cancellation, valueOperation, reaction);

        public override async Task<FlowEndToken> InteractWithUserAsync(IFlowContext context)
        {
            await reaction.ReactToUserAsync(context, new ConsoleOutput($"## Run Sample"));

            return await TryCatchBlock(context, async (context) =>
            {
                if (!context.TryGet<SampleSelected>(out var selected) || selected.id.mode == SampleMode.None)
                {
                    await reaction.ReactToUserAsync(context, new ConsoleOutput($"* Sample not selected."));
                    return await EndInteractAsync(context, reaction, new ConsoleOutput(""));
                }

                var mode = selected.id.mode;

                if (mode == SampleMode.RepeatLast)
                {
                    var lastSelect = lastSelectMemory[context] ?? new SampleID(SampleMode.Parrot);
                    mode = lastSelect.mode;
                }

                return mode switch
                {
                    SampleMode.Parrot => await parrot.InteractWithUserAsync(context),
                    SampleMode.ParrotAuto => await ParrotAuto(context),
                    SampleMode.ParrotAutoAndKill => await ParrotAutoAndKill(context),
                    SampleMode.ParrotColorful => await ParrotColorful(context),
                    SampleMode.ParrotCustomContext => await ParrotCustomContext(context),
                    _ => await EndInteractAsync(context, reaction, new ConsoleOutput("Error")),
                };
            });
        }


        private async Task<FlowEndToken> ParrotAuto(IFlowContext context)
        {
            valueOperation.Text = new ConsoleInputText("I'm Auto Text to Parrot!");
            return await parrotAuto.InteractWithUserAsync(context);
        }

        private async Task<FlowEndToken> ParrotAutoAndKill(IFlowContext context)
        {
            valueOperation.Text = new ConsoleInputText("I'm Auto Text to Parrot! ...?");
            var parrotTask = parrotAuto.InteractWithUserAsync(context);
            var cancelTask = Task.Delay(10000, context.Cancellation.GetToken())
                .ContinueWith(async e =>
                {
                    await e;
                    context.Cancellation.Cancel();
                });

            return await parrotTask;
        }

        private async Task<FlowEndToken> ParrotColorful(IFlowContext context)
        {
            var backgroundColor = ConsoleColor.DarkGreen;
            var mainColor = ConsoleColor.Cyan;
            var errorColor = ConsoleColor.Magenta;
            var cancelColor = ConsoleColor.Yellow;

            using var main = reaction.State.Customize(e => reaction.State = e);
            using var cancel = reaction.CancelState.Customize(e => reaction.CancelState = e);
            using var error = reaction.ErrorState.Customize(e => reaction.ErrorState = e);

            main.Set(foregroundColor: mainColor, backgroundColor: backgroundColor);
            cancel.Set(foregroundColor: errorColor, backgroundColor: backgroundColor);
            error.Set(foregroundColor: cancelColor, backgroundColor: backgroundColor);

            return await parrot.InteractWithUserAsync(context);
        }

        private async Task<FlowEndToken> ParrotCustomContext(IFlowContext context)
        {
            var newContext = new FlowContextGroup(context)
                .AddImmutable(new ParrotHello($"Hello! I'm Parrot with Custom Context, who are you?"), out _);
            context = newContext;
            return await parrot.InteractWithUserAsync(context);
        }
    }
}
