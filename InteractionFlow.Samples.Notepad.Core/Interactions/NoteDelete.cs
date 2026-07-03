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
        public override async Task<FlowEndToken> ExecuteAsync(IFlowContext context)
        {
            await TryCatchBlockAsync(context, async context =>
            {
                using var scope = consoleReaction.GetStateScope();
                scope.State.Update(writeLine: true);

                await Write(context, "# Note Delete");

                await Write(context, "> Loading User data...");
                var userDataResult = await notepadUserDataFiles.LoadUserDataAsync(notepadUserDataPersistence, context);
                if (!userDataResult)
                {
                    throw userDataResult.Exception!;
                }
                var userData = userDataResult.Value!;

                var detaKeySelect = new ConsoleSelectNotepadData(
                    consoleReaction,
                    consoleCursorPositionAccess,
                    consoleOperation,
                    notepadDataFiles,
                    notepadDataPersistence);

                await Write(context, "- Select note to delete:");
                var (select, detaKey) = await detaKeySelect.GetSelectAsync(context, userData);

                if (detaKey.IsEmpty)
                {
                    return await Write(context, "> Cancel.");
                }

                await Write(context, $"> Delete.. - '{notepadDataPersistence.GetViewName(detaKey)}'");
                var deleteResult = await notepadDataPersistence.Delete(detaKey);
                if (!deleteResult)
                {
                    throw deleteResult.Exception!;
                }

                await Write(context, $"> Remove from cache.. - '{detaKey.NoteId}'");

                var removeResult = notepadDataFiles.RemoveWithoutDispose(detaKey);

                await Write(context, $"> Remove from user data.. - '{detaKey.NoteId}'");
                if (!userData.Remove(detaKey))
                {
                    throw new InvalidOperationException("userData.Remove");
                }

                return await Write(context, $"> Note Deleted.");
            });

            return await Write(context, $"> End of Delete");
        }

        private async Task<FlowEndToken> Write(IFlowContext context, string text)
        {
            return await consoleReaction.Write(context, new ConsoleOutput(text));
        }
    }
}
