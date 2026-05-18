using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Core.ReactionPorts;
using InteractionFlow.Samples.Notepad.Entities.Keys;
using InteractionFlow.Samples.Notepad.Interactions.Rules;
using InteractionFlow.Samples.Notepad.StoragePorts;
using InteractionFlow.Standard.Entities;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.OperationPorts;
using InteractionFlow.Standard.ReactionPorts;
using InteractionFlow.Standard.SilentExternalPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Interactions
{
    internal class NoteDelete(
        IExceptionPort<Exception> exceptionPort,
        ICancellationPort cancellationPort,
        IConsoleWriter consoleReaction,
        IConsoleCursorPositionAccess consoleCursorPositionAccess,
        IConsoleOperation consoleOperation,
        INotepadUserDataFiles notepadUserDataFiles) :
        Interaction(exceptionPort, cancellationPort, consoleReaction, consoleCursorPositionAccess, consoleOperation, notepadUserDataFiles)
    {
        public override async Task<FlowEndToken> ExecuteAsync(IFlowContext context)
        {
            await TryCatchBlockAsync(context, async context =>
            {
                using var scope = consoleReaction.GetStateScope();
                scope.State = scope.State.Update(writeLine: true);

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

                var detaKeySelect = new ConsoleSelectNotepadData(consoleReaction, consoleCursorPositionAccess, consoleOperation);

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
