using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Core.ReactionPorts;
using InteractionFlow.Samples.Notepad.Entities.Keys;
using InteractionFlow.Samples.Notepad.StoragePorts;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.OperationPorts;
using InteractionFlow.Standard.ReactionPorts;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Interactions
{
    internal class NoteCreate(
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
                await Write(context, "# Note Create - Enter new note name:");

                if (!context.TryGet(out NotepadUserKey userKey))
                {
                    return await EndInteractAsync(context, consoleReaction, new ConsoleOutput("> Not found NotepadUserKey in context."));
                }

                var dataKey = NotepadDataKey.Empty;

                do
                {
                    var newName = (await consoleOperation.UserOperateTextAsync(context)).text;

                    dataKey = new NotepadDataKey(userKey.Id, newName);

                    if (newName == string.Empty)
                    {
                        return await Write(context, $"> Create Cancel");
                    }
                    else if (!dataKey.IsValid)
                    {
                        await Write(context, "> The name invalid - Retry enter new note name:");
                    }
                    else if (notepadUserDataFiles.Exist(dataKey))
                    {
                        await Write(context, "> The name already exist - Retry enter new note name:");
                    }
                    else
                    {
                        await Write(context, $"> Create - '{newName}'");
                        break;
                    }
                } while (true);

                if (notepadUserDataFiles.TryGetOrCreateDefault(context, out var userData))
                {
                    userData!.Add(dataKey);
                }
                else
                {
                    return await EndInteractAsync(context, consoleReaction, new ConsoleOutput("> Can not Create Note."));
                }

                var saveResult = await notepadUserDataFiles.SaveToPersistent(context);

                if (saveResult)
                {
                    return await EndInteractAsync(context, consoleReaction, new ConsoleOutput($"> Note created as '{dataKey.UserKey.Name}/{dataKey.NoteId}'"));
                }
                else
                {
                    return await EndInteractAsync(context, consoleReaction, new ConsoleOutput("> Can not Save Note."));
                }
            });

            return await EndInteractAsync(context, consoleReaction, new ConsoleOutput($"> End of Create"));
        }

        private async Task<FlowEndToken> Write(IFlowContext context, string text)
        {
            return await EndInteractAsync(context, consoleReaction, new ConsoleOutput(text));
        }
    }
}
