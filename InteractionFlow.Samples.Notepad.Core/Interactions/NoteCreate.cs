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
        protected override bool SelectExisting => false;
        protected override string ActionName => "Create";
        protected override string DraftCancelledMessage => "Create cancelled. Nothing saved.";
        protected override string SavedVerb => "Created";

        protected override async Task<Preparation> PrepareAsync(NotepadKey key)
        {
            var existing = await Notes.GetAllIds();
            if (!existing.Try(out var ids, out var error))
                return new(stopMessage: $"Create failed: {error.Message}");
            if (ids.Any(id => id.User == key.User && id.Note == key.Note))
                return new(stopMessage: "That note already exists. Use Edit instead.");
            return new();
        }
    }
}
