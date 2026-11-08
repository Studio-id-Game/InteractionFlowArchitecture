using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Samples.Notepad.Core.Entities;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts;
using InteractionFlow.Standard.ConsoleSystem.ExternalPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Core.Interactions
{
    public sealed class NoteEdit(
        IConsoleLineInputPort input, IConsoleOutputPort output, IConsoleCancellationBindingPort binding,
        IExceptionPort<Exception> exceptionPort, ICancellationPort cancellationPort, INotepadDataPersistencePort notes,
        INoteEditorOperation editor, IConsoleChoiceOperation choices)
        : NoteEditor(input, output, exceptionPort, cancellationPort, notes, editor, choices, binding)
    {
        protected override async Task<ReactionEnd> ExecuteCoreAsync(IFlowContext flowContext)
        {
            var context = flowContext.GetRequired<NotepadContext>();
            using var bindingScope = binding.Bind(flowContext);
            var keyResult = await ReadKeyAsync(context, existing: true);
            if (!keyResult.Try(out var readKey, out var keyError))
                return await ExceptionPort.HandleExceptionAsync(context, keyError);
            if (readKey.Key is not NotepadKey key)
                return await Output.WriteLineAsync(context, readKey.GetMessage("Edit cancelled."));
            var loaded = await Notes.Load(key, new NotepadData("", ""));
            if (!loaded.Try(out var oldNote, out var loadError))
                return await Output.WriteLineAsync(context, $"Load failed: {loadError.Message}");
            var draft = await ReadDraftAsync(context, oldNote);
            if (draft == null) return await Output.WriteLineAsync(context, "Edit cancelled. Original note kept.");
            context.Cancellation.GetToken().ThrowIfCancellationRequested();
            var saved = await Notes.Save(key, draft);
            return await Output.WriteLineAsync(context, saved.Try(out var error)
                ? $"Saved: {key.Note}" : $"Save failed: {error.Message}");
        }
    }
}
