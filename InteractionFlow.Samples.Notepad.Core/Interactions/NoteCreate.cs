using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts.ReactionPorts;
using InteractionFlow.Core.ExternalPorts.StoragePorts.Entries;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Samples.Notepad.Core.Entities.Keys;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts.PersistencePorts;
using InteractionFlow.Samples.Notepad.Core.Interactions.Rules;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.ExternalPorts.OperationPorts;
using InteractionFlow.Standard.ExternalPorts.ReactionPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Core.Interactions
{
    public class NoteCreate(
        IExceptionPort<Exception> exceptionPort,
        ICancellationPort cancellationPort,
        IConsoleWriter consoleReaction,
        IConsoleOperation consoleOperation,
        INotepadUserDataStoragePort notepadUserDataFiles,
        INotepadUserDataPersistencePort notepadUserDataPersistence,
        INotepadDataStoragePort notepadDataFiles,
        INotepadDataPersistencePort notepadDataPersistence) :
        Interaction(exceptionPort, cancellationPort, consoleReaction, consoleOperation, notepadUserDataFiles)
    {
        protected override async Task<ReactionEnd> ExecuteCoreAsync(IFlowContext context)
        {
            using var scope = consoleReaction.GetStateScope();
            scope.State.Update(writeLine: true);

            await Write(context, "# Note Create");
            await Write(context, "> Loading User data...");

            var notepadDataKey = NotepadDataKey.Empty;

            return await notepadUserDataFiles.LoadUserDataAsync(notepadUserDataPersistence, context)
                .ThenAsync(async userData =>
                {

                    do
                    {
                        await Write(context, "- Enter new note name:");
                        var newNoteName = (await consoleOperation.WaitUserTextAsync(context)).text;

                        if (newNoteName == string.Empty)
                        {
                            await Write(context, "> Create Cancel");
                            return new Exception("Create Cancel");
                        }

                        notepadDataKey = new NotepadDataKey(userData.UserId.Id, newNoteName);

                        if (!notepadDataKey.IsValid)
                        {
                            await Write(context, "> The name invalid - Retry enter new note name:");
                        }
                        else if (userData.Contains(notepadDataKey))
                        {
                            await Write(context, "> The name already exist - Retry enter new note name:");
                        }
                        else
                        {
                            await Write(context, $"> Create - '{newNoteName}'");
                            return Result.Success;
                        }

                    } while (true);
                })
                .ThenAsync(() =>
                {
                    return Task.FromResult(notepadDataFiles.GetOrCreate(notepadDataKey));
                })
                .ThenAsync(async notepadEntity =>
                {
                    var notepadData = notepadEntity.NotepadData;

                    await Write(context, "- Enter new note title:");
                    var newNoteTitle = (await consoleOperation.WaitUserTextAsync(context)).text;
                    notepadData.Title = newNoteTitle;

                    return await notepadEntity.Save(notepadDataPersistence);
                })
                .ResolveAsync(
                onSuccess: async () =>
                {
                    return await Write(context, $"> End of Create : Note Saved as '{notepadDataPersistence.GetViewName(notepadDataKey)}'");
                },
                onFailure: async e =>
                {
                    return await Write(context, $"> Create Error : {e.Message}");
                });
        }

        private async Task<ReactionEnd> Write(IFlowContext context, string text)
        {
            return await consoleReaction.Write(context, new ConsoleOutput(text));
        }
    }
}
