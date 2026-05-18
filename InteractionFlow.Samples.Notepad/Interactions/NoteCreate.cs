using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Core.ReactionPorts;
using InteractionFlow.Samples.Notepad.Entities.Keys;
using InteractionFlow.Samples.Notepad.StoragePorts;
using InteractionFlow.Standard.Entities;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.OperationPorts;
using InteractionFlow.Standard.ReactionPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Interactions
{
    internal class NoteCreate(
        IExceptionPort<Exception> exceptionPort,
        ICancellationPort cancellationPort,
        IConsoleWriter consoleReaction,
        IConsoleOperation consoleOperation,
        INotepadUserDataFiles notepadUserDataFiles) :
        Interaction(exceptionPort, cancellationPort, consoleReaction, consoleOperation, notepadUserDataFiles)
    {
        public override async Task<FlowEndToken> ExecuteAsync(IFlowContext context)
        {
            await TryCatchBlockAsync(context, async context =>
            {
                using var scope = consoleReaction.GetStateScope();
                scope.State = scope.State.Update(writeLine: true);

                await Write(context, "# Note Create - Enter new note name:");

                if (!context.TryGet(out NotepadUserKey userKey))
                {
                    return await Write(context, "> Not found NotepadUserKey in context.");
                }

                var dataKey = NotepadDataKey.Empty;

                do
                {
                    var newName = (await consoleOperation.WaitUserTextAsync(context)).text;

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
                    return await Write(context, "> Can not Create Note.");
                }

                var saveResult = await notepadUserDataFiles.SaveToPersistentAsync(context);

                if (saveResult)
                {
                    return await Write(context, $"> Note created as '{dataKey.UserKey.Name}/{dataKey.NoteId}'");
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
