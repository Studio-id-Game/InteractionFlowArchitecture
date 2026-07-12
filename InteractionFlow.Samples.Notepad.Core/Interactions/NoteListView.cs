using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts.ReactionPorts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts.PersistencePorts;
using InteractionFlow.Samples.Notepad.Core.Interactions.Rules;
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
        INotepadUserDataStoragePort notepadUserDataFiles,
        INotepadDataStoragePort notepadDataFiles,
        INotepadUserDataPersistencePort notepadUserDataPersistence,
        INotepadDataPersistencePort notepadDataPersistence) :
        Interaction(exceptionPort, cancellationPort, consoleReaction, notepadUserDataFiles, notepadDataFiles)
    {
        protected override async Task<ReactionEnd> ExecuteCoreAsync(IFlowContext context)
        {
            using var scope = consoleReaction.GetStateScope();
            scope.State.Update(writeLine: true);

            await Write(context, "# Note List View :");

            await Write(context, "> Loading User data...");

            return await notepadUserDataFiles.LoadUserDataAsync(notepadUserDataPersistence, context)
                    .ThenAsync(async userData =>
                    {
                        Result result = Result.Success;

                        foreach (var noteDataKey in userData)
                        {
                            var fileName = noteDataKey.NoteId;

                            result = await notepadDataFiles.GetOrCreate(noteDataKey).StartAsync()
                               .ThenAsync(async notepadEntry =>
                               {
                                   return await notepadEntry.Load(notepadDataPersistence);
                               })
                               .ThenAsync(async notepadData =>
                               {
                                   var title = notepadData.Title;
                                   await Write(context, $"  - '{fileName}' (title:{title})");

                                   return notepadDataFiles.RemoveWithoutDispose(noteDataKey);
                               });

                            if (!result.Try(out _))
                            {
                                await Write(context, $"  - '{fileName}' (Error)");
                                break;
                            }

                        }

                        return result;
                    }).ResolveAsync(
                    onSuccess: async () =>
                    {
                        return await Write(context, "> End of List.");
                    },
                    onFailure: async e =>
                    {
                        return await Write(context, $"> List Error : {e.Message}");
                    });
        }

        private async Task<ReactionEnd> Write(IFlowContext context, string text)
        {
            return await consoleReaction.Write(context, new ConsoleOutput(text));
        }
    }
}
