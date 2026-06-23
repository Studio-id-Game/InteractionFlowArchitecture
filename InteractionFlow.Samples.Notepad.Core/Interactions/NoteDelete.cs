using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts.ReactionPorts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Samples.Notepad.Core.Entities.Keys;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts;
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
        INotepadUserDataFiles notepadUserDataFiles,
        INotepadDataFiles notepadDataFiles) :
        Interaction(exceptionPort, cancellationPort, consoleReaction, consoleCursorPositionAccess, consoleOperation, notepadUserDataFiles, notepadDataFiles)
    {
        public override async Task<FlowEndToken> ExecuteAsync(IFlowContext context)
        {
            await TryCatchBlockAsync(context, async context =>
            {
                using var scope = consoleReaction.GetStateScope();
                scope.State.Update(writeLine: true);

                await Write(context, "# Note Delete - Select note to delete:");

                if (!context.TryGet(out NotepadUserKey userKey))
                {
                    return await Write(context, "> Not found NotepadUserKey in context.");
                }

                var userDataResult = await notepadUserDataFiles.LoadFromPersistentAsync(context);

                if (!userDataResult)
                {
                    return await Write(context, "> Not found NotepadUserData.");
                }

                var userData = userDataResult.Value!;

                var detaKeySelect = new ConsoleSelectNotepadData(consoleReaction, consoleCursorPositionAccess, consoleOperation, notepadDataFiles);

                var (select, detaKey) = await detaKeySelect.GetSelectAsync(context, userData);

                if (detaKey.IsEmpty)
                {
                    return await Write(context, "> Cancel.");
                }

                if (userData.Remove(detaKey))
                {
                    await Write(context, $"> Remove - '{detaKey.UserKey.Name}/{detaKey.NoteId}'");
                }
                else
                {
                    return await Write(context, "> Can not Remove Note.");
                }

                if (await notepadUserDataFiles.SaveToPersistentAsync(context))
                {
                    return await Write(context, $"> Save changed");
                }
                else
                {
                    return await Write(context, "> Can not Save Note.");
                }
            });

            return await Write(context, $"> End of Delete");
        }

        private async Task<FlowEndToken> Write(IFlowContext context, string text)
        {
            return await consoleReaction.Write(context, new ConsoleOutput(text));
        }
    }
}
