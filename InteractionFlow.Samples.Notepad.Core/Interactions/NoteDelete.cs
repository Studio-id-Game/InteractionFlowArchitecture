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
    public sealed class NoteDelete(
        IConsoleOutputPort output, IConsoleCancellationBindingPort binding,
        IExceptionPort<Exception> exceptionPort, ICancellationPort cancellationPort, INotepadDataPersistencePort notes,
        IConsoleChoiceOperation choices)
        : Interaction(exceptionPort, cancellationPort, output, binding, notes, choices)
    {
        protected override async ValueTask<ReactionEnd> ExecuteCoreAsync(IFlowContext flowContext)
        {
            var context = flowContext.GetRequired<NotepadContext>();
            using var bindingScope = binding.Bind(flowContext);
            var listed = await notes.GetAllIds();
            if (!listed.HasValue(out var ids))
                return await output.WriteLineAsync(context, $"List failed: {listed.Error!.Message}");
            var available = ids.Where(id => id.User == context.User)
                .OrderBy(id => id.Note, StringComparer.Ordinal).ToArray();
            if (available.Length == 0) return await output.WriteLineAsync(context, "No notes available.");
            var labels = new string[available.Length];
            for (var i = 0; i < available.Length; i++)
            {
                context.Cancellation.GetToken().ThrowIfCancellationRequested();
                var loaded = await notes.Load(available[i], new NotepadData("", ""));
                labels[i] = available[i].Note + ": " + (loaded.HasValue(out var note) ? note.Title : "[Unreadable]");
            }
            var selection = await choices.SelectAsync(context, "Select a note to delete",
                [.. available.Select(id => id.Note)], labels);
            if (!selection.HasValue(out var choice))
                    return await ExceptionPort.HandleExceptionAsync(context, selection.Error!);
            var selected = choice.Index;
            if (selected is not >= 0) return await output.WriteLineAsync(context, "Delete cancelled.");
            var id = available[selected.Value];
            var confirmation = await choices.SelectAsync(context, $"Delete '{id.Note}'?", ["No", "Yes"]);
            if (!confirmation.HasValue(out var confirm))
                    return await ExceptionPort.HandleExceptionAsync(context, confirmation.Error!);
            if (confirm.Index != 1) return await output.WriteLineAsync(context, "Delete cancelled.");
            context.Cancellation.GetToken().ThrowIfCancellationRequested();
            var result = await notes.Delete(id);
            return await output.WriteLineAsync(context, result.HasError(out var deleteError)
                ? $"Delete failed: {deleteError.Message}" : $"Deleted: {id.Note}");
        }
    }
}
