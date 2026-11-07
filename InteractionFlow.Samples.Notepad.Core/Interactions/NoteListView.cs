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
    public sealed class NoteListView(
        IConsoleLineInputPort input, IConsoleOutputPort output, IConsoleCancellationBindingPort binding,
        IExceptionPort<Exception> exceptionPort, ICancellationPort cancellationPort, INotepadDataPersistencePort notes,
        IConsoleKeyInputPort keys, IConsoleEnvironmentPort environment)
        : NotepadInteraction(input, output, binding, exceptionPort, cancellationPort, notes, keys, environment)
    {
        protected override async Task<ReactionEnd> RunAsync(NotepadContext context)
        {
            var result = await notes.GetAllIds();
            if (!result.Try(out var ids, out var error))
                return await Output.WriteLineAsync(context, $"List failed: {error.Message}");
            await WriteAsync(context, $"# Notes — {NotepadRules.UserName(context.User!)}");
            foreach (var id in ids.Where(id => id.User == context.User).OrderBy(id => id.Note, StringComparer.Ordinal))
            {
                context.Cancellation.GetToken().ThrowIfCancellationRequested();
                var loaded = await notes.Load(id, new NotepadData("", ""));
                var label = loaded.Try(out var note, out var loadError)
                    ? note.Title : $"[Unreadable: {loadError.Message}]";
                await WriteAsync(context, $"- {id.Note}: {label}");
            }
            await WriteAsync(context, "End of list.");
            if (!environment.IsInputRedirected && !environment.IsOutputRedirected)
            {
                await WriteAsync(context, "Press any key to return to the menu.");
                await keys.ReadKeyAsync(context);
            }
            return await Output.WriteLineAsync(context, "");
        }
    }
}
