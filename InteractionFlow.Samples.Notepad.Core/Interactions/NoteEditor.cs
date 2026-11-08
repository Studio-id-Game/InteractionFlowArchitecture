using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
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
        IConsoleLineInputPort input, IConsoleOutputPort output, IConsoleCancellationBindingPort binding,
        IExceptionPort<Exception> exceptionPort, ICancellationPort cancellationPort, INotepadDataPersistencePort notes,
        INoteEditorOperation editor, IConsoleChoiceOperation choices,
        params InteractionFlow.Core.Entities.Architectures.IDependencyNode[] dependency)
        : Interaction(exceptionPort, cancellationPort,
            [input, output, binding, notes, editor, choices, .. dependency])
    {
        protected INotepadDataPersistencePort Notes => notes;
        protected abstract bool SelectExisting { get; }
        protected abstract string ActionName { get; }
        protected abstract string DraftCancelledMessage { get; }
        protected abstract string SavedVerb { get; }

        // Preparation returns data or a diagnostic; only the shared execution emits the final reaction.
        protected sealed class Preparation(NotepadData? original = null, string? stopMessage = null)
        {
            public NotepadData? Original { get; } = original;
            public string? StopMessage { get; } = stopMessage;
        }

        protected abstract Task<Preparation> PrepareAsync(NotepadKey key);

        protected sealed override async Task<ReactionEnd> ExecuteCoreAsync(IFlowContext flowContext)
        {
            var context = flowContext.GetRequired<NotepadContext>();
            using var bindingScope = binding.Bind(flowContext);
            var keyResult = await ReadKeyAsync(context, SelectExisting);
            if (!keyResult.Try(out var readKey, out var keyError))
                return await ExceptionPort.HandleExceptionAsync(context, keyError);
            if (readKey.Key is not NotepadKey key)
                return await output.WriteLineAsync(context, readKey.GetMessage($"{ActionName} cancelled."));

            var preparation = await PrepareAsync(key);
            if (preparation.StopMessage != null)
                return await output.WriteLineAsync(context, preparation.StopMessage);

            var edited = await editor.EditAsync(context, preparation.Original);
            if (!edited.Try(out var editValue, out var editError))
                return await ExceptionPort.HandleExceptionAsync(context, editError);
            var draft = editValue.Note;
            if (draft == null) return await output.WriteLineAsync(context, DraftCancelledMessage);
            context.Cancellation.GetToken().ThrowIfCancellationRequested();
            var saved = await notes.Save(key, draft);
            return await output.WriteLineAsync(context, saved.Try(out var error)
                ? $"{SavedVerb}: {key.Note}" : $"Save failed: {error.Message}");
        }

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
                var selection = await choices.SelectAsync(context, "Select a note",
                    [.. available.Select(id => id.Note)], labels);
                if (!selection.Try(out var choice, out var choiceError)) return choiceError;
                var selected = choice.Index;
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
