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
        : NoteEditor(input, output, binding, exceptionPort, cancellationPort, notes, editor, choices)
    {
        protected override bool SelectExisting => true;
        protected override string ActionName => "Edit";
        protected override string DraftCancelledMessage => "Edit cancelled. Original note kept.";
        protected override string SavedVerb => "Saved";

        protected override async Task<Preparation> PrepareAsync(NotepadKey key)
        {
            var loaded = await Notes.Load(key, new NotepadData("", ""));
            return loaded.Try(out var note, out var error)
                ? new(original: note) : new(stopMessage: $"Load failed: {error.Message}");
        }
    }
}
