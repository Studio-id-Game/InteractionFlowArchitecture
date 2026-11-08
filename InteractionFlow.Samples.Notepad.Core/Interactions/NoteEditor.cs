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

        private async Task WriteAsync(NotepadContext context, string text)
        {
            var end = await output.WriteLineAsync(context, text);
            if (end.HasException) throw end.Exception!;
        }

        private async Task<string?> ReadAsync(NotepadContext context, string prompt)
        {
            await WriteAsync(context, prompt);
            var text = await input.ReadLineAsync(context);
            if (text == null) context.ExitRequested = true;
            return text;
        }

        protected Task<NotepadData?> ReadDraftAsync(NotepadContext context, NotepadData? oldNote)
            => editor.EditAsync(context, oldNote);

        protected async Task<NotepadKey?> ReadKeyAsync(NotepadContext context, bool existing = false)
        {
            if (existing)
            {
                var result = await notes.GetAllIds();
                if (!result.Try(out var ids, out var error))
                {
                    await WriteAsync(context, $"List failed: {error.Message}");
                    return null;
                }
                var available = ids.Where(id => id.User == context.User)
                    .OrderBy(id => id.Note, StringComparer.Ordinal).ToArray();
                if (available.Length == 0)
                {
                    await WriteAsync(context, "No notes available.");
                    return null;
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
                return selected is >= 0 ? available[selected.Value] : (NotepadKey?)null;
            }
            var text = await ReadAsync(context, "Note ID (blank cancels):");
            if (string.IsNullOrWhiteSpace(text)) return null;
            var id = NotepadRules.NormalizeId(text);
            if (!NotepadRules.IsValidId(id))
            {
                await WriteAsync(context, "Invalid note ID. Use 1–64 letters, digits, underscores or hyphens.");
                return null;
            }
            return new NotepadKey(context.User!, id);
        }
    }
}
