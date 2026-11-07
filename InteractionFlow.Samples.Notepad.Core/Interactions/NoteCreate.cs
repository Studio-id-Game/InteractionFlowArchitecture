using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Samples.Notepad.Core.Entities;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts;
using InteractionFlow.Standard.ConsoleSystem.ExternalPorts;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Core.Interactions
{
    public sealed class NoteCreate(
        IConsoleLineInputPort input, IConsoleOutputPort output, IConsoleCancellationBindingPort binding,
        IExceptionPort<Exception> exceptionPort, ICancellationPort cancellationPort, INotepadDataPersistencePort notes,
        INoteEditorOperation editor, IConsoleChoiceOperation choices)
        : NoteEditor(input, output, binding, exceptionPort, cancellationPort, notes, editor, choices)
    {
        protected override async Task<ReactionEnd> RunAsync(NotepadContext context)
        {
            var key = await ReadKeyAsync(context);
            if (key == null) return await Output.WriteLineAsync(context, "Create cancelled.");
            var existing = await Notes.GetAllIds();
            if (!existing.Try(out var ids, out var listError))
                return await Output.WriteLineAsync(context, $"Create failed: {listError.Message}");
            if (ids.Any(id => id.User == key.Value.User && id.Note == key.Value.Note))
                return await Output.WriteLineAsync(context, "That note already exists. Use Edit instead.");
            var draft = await ReadDraftAsync(context, null);
            if (draft == null) return await Output.WriteLineAsync(context, "Create cancelled. Nothing saved.");
            context.Cancellation.GetToken().ThrowIfCancellationRequested();
            var saved = await Notes.Save(key.Value, draft);
            return await Output.WriteLineAsync(context, saved.Try(out var error)
                ? $"Created: {key.Value.Note}" : $"Save failed: {error.Message}");
        }
    }
}
