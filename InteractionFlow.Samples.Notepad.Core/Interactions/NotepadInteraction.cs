using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Samples.Notepad.Core.Entities;
using InteractionFlow.Standard.ConsoleSystem.ExternalPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Core.Interactions
{
    // Common console execution boundary; each operation below retains its own application logic.
    public abstract class NotepadInteraction(
        IConsoleLineInputPort input, IConsoleOutputPort output, IConsoleCancellationBindingPort binding,
        IExceptionPort<Exception> exceptionPort, ICancellationPort cancellationPort,
        params IDependencyNode[] dependency)
        : Interaction(exceptionPort, cancellationPort, [input, output, binding, .. dependency])
    {
        protected IConsoleOutputPort Output => output;
        protected IConsoleLineInputPort Input => input;

        protected sealed override async Task<ReactionEnd> ExecuteCoreAsync(IFlowContext context)
        {
            if (!context.TryGet<NotepadContext>(out var notepad))
                throw new InvalidOperationException("No Notepad context.");
            using var bindingScope = binding.Bind(context);
            return await RunAsync(notepad);
        }

        protected abstract Task<ReactionEnd> RunAsync(NotepadContext context);

        protected async Task WriteAsync(NotepadContext context, string text)
        {
            var end = await output.WriteLineAsync(context, text);
            // An output failure must stop the operation before accepting input or changing files.
            if (end.HasException) throw end.Exception!;
        }

        protected async Task<string?> ReadAsync(NotepadContext context, string prompt)
        {
            await WriteAsync(context, prompt);
            var text = await input.ReadLineAsync(context);
            if (text == null) context.ExitRequested = true;
            return text;
        }
    }
}
