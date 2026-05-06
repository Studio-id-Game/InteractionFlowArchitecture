using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Core.ReactionPorts;
using InteractionFlow.Samples.Notepad.Entities.Keys;
using InteractionFlow.Samples.Notepad.Interactions.Rules;
using InteractionFlow.Samples.Notepad.StoragePorts;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.OperationPorts;
using InteractionFlow.Standard.ReactionPorts;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Interactions
{
    internal class NoteDelete(
        IExceptionPort exceptionPort,
        ICancellationPort cancellationPort,
        IConsoleReaction consoleReaction,
        IConsoleOperation consoleOperation,
        INotepadUserDataFiles notepadUserDataFiles) :
        Interaction(exceptionPort, cancellationPort)
    {
        public override async Task<FlowEndToken> InteractWithUserAsync(IFlowContext context)
        {
            await TryCatchBlock(context, async context =>
            {
                await Write(context, "# Note Delete - Select note to delete:");

                if (!context.TryGet(out NotepadUserKey userKey))
                {
                    return await EndInteractAsync(context, consoleReaction, new ConsoleOutput("> Not found NotepadUserKey in context."));
                }

                var userDataResult = await notepadUserDataFiles.LoadFromPersistent(context);

                if (!userDataResult)
                {
                    return await EndInteractAsync(context, consoleReaction, new ConsoleOutput("> Not found NotepadUserData."));
                }

                var userData = userDataResult.Value!;

                var detaKeySelect = new ConsoleSelectNotepadData(consoleReaction, consoleOperation);

                var (select, detaKey) = await detaKeySelect.GetSelectAsync(context, userData);

                if (detaKey.IsEmpty)
                {
                    return await EndInteractAsync(context, consoleReaction, new ConsoleOutput("> Cancel."));
                }

                if (userData.Remove(detaKey))
                {
                    await Write(context, $"> Remove - '{detaKey.UserKey.Name}/{detaKey.NoteId}'");
                }
                else
                {
                    return await EndInteractAsync(context, consoleReaction, new ConsoleOutput("> Can not Remove Note."));
                }

                if (await notepadUserDataFiles.SaveToPersistent(context))
                {
                    return await EndInteractAsync(context, consoleReaction, new ConsoleOutput($"> Save changed"));
                }
                else
                {
                    return await EndInteractAsync(context, consoleReaction, new ConsoleOutput("> Can not Save Note."));
                }
            });

            return await EndInteractAsync(context, consoleReaction, new ConsoleOutput($"> End of Delete"));
        }

        private async Task<FlowEndToken> Write(IFlowContext context, string text)
        {
            return await EndInteractAsync(context, consoleReaction, new ConsoleOutput(text));
        }
    }
}
