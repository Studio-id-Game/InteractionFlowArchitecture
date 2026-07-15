using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts.ReactionPorts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Samples.Parrot.Entities;
using InteractionFlow.Samples.Parrot.Entities.ParrotContexts;
using InteractionFlow.Samples.Parrot.Entities.SampleContexts;
using InteractionFlow.Samples.Parrot.ExternalPorts.StoragePorts;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.ExternalPorts.OperationPorts;
using InteractionFlow.Standard.ExternalPorts.ReactionPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Parrot.Interactions
{
    internal class RunSample(
        IExceptionPort<Exception> exception,
        ICancellationPort cancellation,
        IConsoleOperation operation,
        IConsoleOperation.IDummy operationDummy,
        IConsoleWriter reaction,
        ILastSelectMemory lastSelectMemory)
        : Interaction(exception, cancellation, operation, operationDummy, reaction, lastSelectMemory), IInteraction
    {
        private readonly Parrot parrot = new(exception, cancellation, operation, reaction);
        private readonly Parrot parrotAuto = new(exception, cancellation, operationDummy, reaction);

        protected override async Task<ReactionEnd> ExecuteCoreAsync(IFlowContext context)
        {
            await reaction.Write(context, new ConsoleOutput($"## Run Sample (Press Ctrl + C to cancel the sample.)"));

            if (!context.TryGet<RefEntity<SampleSelected>>(out var selected) || selected.Value.id.mode == SampleMode.None)
            {
                await reaction.Write(context, new ConsoleOutput($"* Sample not selected."));
                return await reaction.Write(context, new ConsoleOutput(""));
            }

            var mode = selected.Value.id.mode;

            if (mode == SampleMode.RepeatLast)
            {
                lastSelectMemory.GetKey(context)
                    .Then(lastSelectMemory.GetOrCreate)
                    .Then(lastMode =>
                    {
                        mode = lastMode.Value.mode;
                        return lastMode.AsResult();
                    })
                    .ThrowIfError();
            }

            return mode switch
            {
                SampleMode.Parrot => await Parrot(context),
                SampleMode.ParrotAuto => await ParrotAuto(context),
                SampleMode.ParrotAutoAndKill => await ParrotAutoAndKill(context),
                SampleMode.ParrotColorful => await ParrotColorful(context),
                SampleMode.ParrotCustomContext => await ParrotCustomContext(context),
                _ => await reaction.Write(context, new ConsoleOutput("Error")),
            };
        }


        private async Task<ReactionEnd> Parrot(IFlowContext context)
        {
            return await NestedExecuteAsync(parrot, context);
        }

        private async Task<ReactionEnd> ParrotAuto(IFlowContext context)
        {
            operationDummy.DummyText = new ConsoleInputText("I'm Auto Text to Parrot!");
            return await NestedExecuteAsync(parrotAuto, context);
        }

        private async Task<ReactionEnd> ParrotAutoAndKill(IFlowContext context)
        {
            operationDummy.DummyText = new ConsoleInputText("I'm Auto Text to Parrot! ...?");
            var parrotTask = NestedExecuteAsync(parrotAuto, context);
            var cancelTask = Task.Delay(10000, context.Cancellation.GetToken())
                .ContinueWith(async e =>
                {
                    await e;
                    context.Cancellation.Cancel();
                });

            return await parrotTask;
        }

        private async Task<ReactionEnd> ParrotColorful(IFlowContext context)
        {
            var mainBackColor = ConsoleColor.DarkCyan;
            var mainColor = ConsoleColor.Cyan;
            var errorBackColor = ConsoleColor.DarkMagenta;
            var errorColor = ConsoleColor.Red;
            var cancelBackColor = ConsoleColor.DarkYellow;
            var cancelColor = ConsoleColor.Yellow;
            var opBackColor = ConsoleColor.DarkBlue;
            var opColor = ConsoleColor.Blue;

            using var main = reaction.GetStateScope();
            reaction.State.Update(foregroundColor: mainColor, backgroundColor: mainBackColor);

            var _cancellation = CancellationPort as IConsoleReaction;
            using var cancel = _cancellation?.GetStateScope();
            _cancellation?.State.Update(foregroundColor: cancelColor, backgroundColor: cancelBackColor);

            var _exception = ExceptionPort as IConsoleReaction;
            using var error = _exception?.GetStateScope();
            _exception?.State.Update(foregroundColor: errorColor, backgroundColor: errorBackColor);

            using var op = operation.GetStateScope();
            operation.State.Update(foregroundColor: opColor, backgroundColor: opBackColor);

            return await NestedExecuteAsync(parrot, context);
        }

        private async Task<ReactionEnd> ParrotCustomContext(IFlowContext context)
        {
            var newContext = new ScopedFlowContext(context)
                .With(new RefEntity<ParrotHello>(new($"Hello! I'm Parrot with Custom Context, who are you?")));
            context = newContext;
            return await NestedExecuteAsync(parrot, context);
        }
    }
}
