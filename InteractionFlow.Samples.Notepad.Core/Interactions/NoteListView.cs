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
        public override async Task<FlowEndToken> ExecuteAsync(IFlowContext context)
        {
            return await TryCatchBlockAsync(context, async context =>
            {
                using var scope = consoleReaction.GetStateScope();
                scope.State.Update(writeLine: true);

                await Write(context, "# Note List View :");

                await Write(context, "> Loading User data...");
                var userDataResult = await notepadUserDataFiles.LoadUserDataAsync(notepadUserDataPersistence, context);
                if (!userDataResult)
                {
                    throw userDataResult.Exception!;
                }

                var userData = userDataResult.Value!;

                foreach (var noteDataKey in userData)
                {
                    var notepadDataResult = notepadDataFiles.GetOrCreate(noteDataKey);
                    if (!notepadDataResult) continue;
                    var notepadData = notepadDataResult.Value!;

                    var loadResult = await notepadData.Load(notepadDataPersistence);
                    if (!loadResult) continue;

                    var fileName = noteDataKey.NoteId;
                    var title = notepadData.Value!.Title;
                    await Write(context, $"  - '{fileName}' (title:{title})");

                    notepadDataFiles.RemoveWithoutDispose(noteDataKey);
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
