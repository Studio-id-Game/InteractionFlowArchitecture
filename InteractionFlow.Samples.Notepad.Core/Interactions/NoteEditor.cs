using InteractionFlow.Core.Entities;
using InteractionFlow.Core.ExternalPorts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Samples.Notepad.Core.Entities;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts;
using InteractionFlow.Standard.ConsoleSystem.ExternalPorts;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Core.Interactions
{
    public abstract class NoteEditor(
        IConsoleLineInputPort input, IConsoleOutputPort output,
        IExceptionPort<Exception> exceptionPort, ICancellationPort cancellationPort, INotepadDataPersistencePort notes,
        INoteEditorOperation editor, IConsoleChoiceOperation choices,
        params InteractionFlow.Core.Entities.Architectures.IDependencyNode[] dependency)
        : Interaction(exceptionPort, cancellationPort,
            [input, output, notes, editor, choices, .. dependency])
    {
        protected INotepadDataPersistencePort Notes => notes;
        protected IConsoleOutputPort Output => output;

        protected Task<NotepadData?> ReadDraftAsync(NotepadContext context, NotepadData? oldNote)
            => editor.EditAsync(context, oldNote);

        // Acquisition returns data and status; the caller decides the final reaction.
        protected async Task<Result<ReadKeyValue>> ReadKeyAsync(
            NotepadContext context, bool existing = false)
        {
            if (existing)
            {
                var result = await notes.GetAllIds();
                if (!result.Try(out var ids, out var error))
                {
                    return error;
                }
                var available = ids.Where(id => id.User == context.User)
                    .OrderBy(id => id.Note, StringComparer.Ordinal).ToArray();
                if (available.Length == 0)
                {
                    return ReadKeyValue.NoNotes;
                }
                var labels = new string[available.Length];
                for (var i = 0; i < available.Length; i++)
                {
                    context.Cancellation.GetToken().ThrowIfCancellationRequested();
                    var loaded = await notes.Load(available[i], new NotepadData("", ""));
                    labels[i] = available[i].Note + ": " + (loaded.Try(out var note, out _) ? note.Title : "[Unreadable]");
                }
                var selected = await choices.SelectAsync(context, "Select a note",
                    [.. available.Select(id => id.Note)], labels);
                return selected is >= 0
                    ? ReadKeyValue.Selected(available[selected.Value]) : ReadKeyValue.Cancelled;
            }
            var prompt = await output.WriteLineAsync(context, "Note ID (blank cancels):");
            if (!prompt.Result.Try(out var promptError)) return promptError;
            var text = await input.ReadLineAsync(context);
            if (text == null) context.ExitRequested = true;
            if (string.IsNullOrWhiteSpace(text)) return ReadKeyValue.Cancelled;
            var id = NotepadRules.NormalizeId(text);
            if (!NotepadRules.IsValidId(id))
            {
                return ReadKeyValue.InvalidId;
            }
            return ReadKeyValue.Selected(new NotepadKey(context.User!, id));
        }
    }
}
