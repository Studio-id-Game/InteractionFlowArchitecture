using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts.ReactionPorts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts.PersistencePorts;
using InteractionFlow.Samples.Notepad.Core.Interactions.Rules;
using InteractionFlow.Standard.Entities;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.ExternalPorts.OperationPorts;
using InteractionFlow.Standard.ExternalPorts.ReactionPorts;
using InteractionFlow.Standard.ExternalPorts.SilentExternalPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Core.Interactions
{
    public class NoteDelete(
        IExceptionPort<Exception> exceptionPort,
        ICancellationPort cancellationPort,
        IConsoleWriter consoleReaction,
        IConsoleCursorPositionAccess consoleCursorPositionAccess,
        IConsoleOperation consoleOperation,
        INotepadUserDataStoragePort notepadUserDataFiles,
        INotepadUserDataPersistencePort notepadUserDataPersistence,
        INotepadDataStoragePort notepadDataFiles,
        INotepadDataPersistencePort notepadDataPersistence) :
        Interaction(exceptionPort, cancellationPort, consoleReaction, consoleCursorPositionAccess, consoleOperation, notepadUserDataFiles, notepadDataFiles)
    {
        protected override async Task<ReactionEnd> ExecuteCoreAsync(IFlowContext context)
        {
            using var scope = consoleReaction.GetStateScope();
            scope.State.Update(writeLine: true);

            await Write(context, "# Note Delete");

            await Write(context, "> Loading User data...");
            return await notepadUserDataFiles.LoadUserDataAsync(notepadUserDataPersistence, context)
                .ThenAsync(async userData =>
                {

                    var detaKeySelect = new ConsoleSelectNotepadData(
                        consoleReaction,
                        consoleCursorPositionAccess,
                        consoleOperation,
                        notepadDataFiles,
                        notepadDataPersistence);

                    await Write(context, "- Select note to delete:");
                    var (select, dataKey) = await detaKeySelect.GetSelectAsync(context, userData, true);
                    if (dataKey.IsEmpty)
                    {
                        return new Exception("Cancel");
                    }

                    await Write(context, $"> Delete.. - '{notepadDataPersistence.GetViewName(dataKey)}'");
                    return await notepadDataPersistence.Delete(dataKey)
                        .ThenAsync(() => Task.FromResult((userData, dataKey).AsResult()));
                })
                .ThenAsync(async (value) =>
                {
                    var (userData, dataKey) = value;
                    if (notepadDataFiles.ContainsKey(dataKey))
                    {
                        await Write(context, $"> Remove from cache.. - '{dataKey.NoteId}'");
                        notepadDataFiles.RemoveWithoutDispose(dataKey);
                    }

                    return value.AsResult();
                })
                .ThenAsync(async value =>
                {
                    var (userData, dataKey) = value;
                    if (userData.Contains(dataKey))
                    {
                        await Write(context, $"> Remove from user data.. - '{dataKey.NoteId}'");
                        userData.Remove(dataKey);
                    }

                    return Result.Success;
                })
                .ResolveAsync(
                    onSuccess: async () =>
                    {
                        return await Write(context, $"> End of Delete : Note Deleted.");
                    },
                    onFailure: async e =>
                    {
                        return await Write(context, $"> End of Delete : Note can not Deleted : {e.Message}, {e.StackTrace}");
                    });
        }

        private async Task<ReactionEnd> Write(IFlowContext context, string text)
        {
            return await consoleReaction.Write(context, new ConsoleOutput(text));
        }
    }
}
