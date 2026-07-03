using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts.ReactionPorts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Samples.Notepad.Core.Entities.Keys;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts.PersistencePorts;
using InteractionFlow.Samples.Notepad.Core.Interactions.Rules;
using InteractionFlow.Standard.Entities;
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
        public override async Task<FlowEndToken> ExecuteAsync(IFlowContext context)
        {
            await TryCatchBlockAsync(context, async context =>
            {
                using var scope = consoleReaction.GetStateScope();
                scope.State.Update(writeLine: true);

                await Write(context, "# Note Create");
                await Write(context, "> Loading User data...");
                var userDataResult = await notepadUserDataFiles.LoadUserDataAsync(notepadUserDataPersistence, context);
                if (!userDataResult)
                {
                    throw userDataResult.Exception!;
                }
                var userData = userDataResult.Value!;

                var notepadDataKey = NotepadDataKey.Empty;

                do
                {
                    await Write(context, "- Enter new note name:");
                    var newNoteName = (await consoleOperation.WaitUserTextAsync(context)).text;

                    if (newNoteName == string.Empty)
                    {
                        return await Write(context, $"> Create Cancel");
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
                        break;
                    }

                } while (true);

                var notepadEntityResult = notepadDataFiles.GetOrCreate(notepadDataKey);
                if (!notepadEntityResult)
                {
                    throw new InvalidOperationException("> Can not get or create notepad data.");
                }

                var notepadEntity = notepadEntityResult.Value!;
                var notepadData = notepadEntity.NotepadData;

                await Write(context, "- Enter new note title:");
                var newNoteTitle = (await consoleOperation.WaitUserTextAsync(context)).text;
                notepadData.Title = newNoteTitle;

                var saveResult = await notepadEntity.Save(notepadDataPersistence);

                if (saveResult)
                {
                    return await Write(context, $"> Note Saved as '{notepadDataPersistence.GetViewName(notepadDataKey)}'");
                }
                else
                {
                    return await Write(context, "> Can not Save Note.");
                }
            });

            return await Write(context, $"> End of Create");
        }

        private async Task<FlowEndToken> Write(IFlowContext context, string text)
        {
            return await consoleReaction.Write(context, new ConsoleOutput(text));
        }
    }
}
