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
    public sealed class NoteListView(
        IConsoleOutputPort output, IConsoleCancellationBindingPort binding,
        IExceptionPort<Exception> exceptionPort, ICancellationPort cancellationPort, INotepadDataPersistencePort notes,
        IConsoleKeyInputPort keys, IConsoleEnvironmentPort environment)
        : Interaction(exceptionPort, cancellationPort, output, binding, notes, keys, environment)
    {
        protected override async ValueTask<ReactionEnd> ExecuteCoreAsync(IFlowContext flowContext)
        {
            var context = flowContext.GetRequired<NotepadContext>();
            using var bindingScope = binding.Bind(flowContext);
            var result = await notes.GetAllIds();
            if (!result.Try(out var ids, out var error))
                return await output.WriteLineAsync(context, $"List failed: {error.Message}");
            var end = await output.WriteLineAsync(context, $"# Notes — {NotepadRules.UserName(context.User!)}");
            if (end.HasException) return end;
            foreach (var id in ids.Where(id => id.User == context.User).OrderBy(id => id.Note, StringComparer.Ordinal))
            {
                context.Cancellation.GetToken().ThrowIfCancellationRequested();
                var loaded = await notes.Load(id, new NotepadData("", ""));
                var label = loaded.Try(out var note, out var loadError)
                    ? note.Title : $"[Unreadable: {loadError.Message}]";
                end = await output.WriteLineAsync(context, $"- {id.Note}: {label}");
                if (end.HasException) return end;
            }
            end = await output.WriteLineAsync(context, "End of list.");
            if (end.HasException) return end;
            if (!environment.IsInputRedirected && !environment.IsOutputRedirected)
            {
                end = await output.WriteLineAsync(context, "Press any key to return to the menu.");
                if (end.HasException) return end;
                await keys.ReadKeyAsync(context);
            }
            return await output.WriteLineAsync(context, "");
        }

    }
}
