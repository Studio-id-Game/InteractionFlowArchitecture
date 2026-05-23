using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts.ReactionPorts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Samples.Notepad.Core.Entities.Keys;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts;
using InteractionFlow.Standard.Entities;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.ExternalPorts.ReactionPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Core.Interactions
{
    public class NoteListView(
        IExceptionPort<Exception> exceptionPort,
        ICancellationPort cancellationPort,
        IConsoleWriter consoleReaction,
        INotepadUserDataFiles notepadUserDataFiles,
        INotepadDataFiles notepadDataFiles) :
        Interaction(exceptionPort, cancellationPort, consoleReaction, notepadUserDataFiles, notepadDataFiles)
    {
        public override async Task<FlowEndToken> ExecuteAsync(IFlowContext context)
        {
            return await TryCatchBlockAsync(context, async context =>
            {
                using var scope = consoleReaction.GetStateScope();
                scope.State.Update(writeLine: true);

                await Write(context, "# Note List View :");

                var userDataLoadResult = await notepadUserDataFiles.LoadFromPersistentAsync(context);

                if (!userDataLoadResult)
                {
                    return await Write(context, "> Can not load NotepadUserData.");
                }

                var userData = userDataLoadResult.Value!;

                var noteContext = new FlowContextGroup(context)
                    .Add(NotepadDataKey.Empty, out var noteDataKeyContext);

                foreach (var noteDataKey in userData)
                {
                    noteDataKeyContext.Value = noteDataKey;
                    var noteDataLoadResult = await notepadDataFiles.LoadFromPersistentAsync(noteContext);
                    if (noteDataLoadResult)
                    {
                        var fileName = noteDataKey.NoteId;
                        var title = noteDataLoadResult.Value!.Title;
                        await Write(context, $"  - '{fileName}' (title:{title})");
                    }
                }

                return await Write(context, "> End of List.");
            });
        }

        private async Task<FlowEndToken> Write(IFlowContext context, string text)
        {
            return await consoleReaction.Write(context, new ConsoleOutput(text));
        }
    }
}
