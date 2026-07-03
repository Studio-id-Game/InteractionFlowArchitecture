using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts.ReactionPorts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Samples.Notepad.Core.Entities.Contexts;
using InteractionFlow.Samples.Notepad.Core.Entities.Keys;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts.PersistencePorts;
using InteractionFlow.Samples.Notepad.Core.Interactions.Rules;
using InteractionFlow.Standard.Entities;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.ExternalPorts.OperationPorts;
using InteractionFlow.Standard.ExternalPorts.ReactionPorts;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Core.Interactions
{
    public class Login(
        IExceptionPort<Exception> exceptionPort,
        ICancellationPort cancellationPort,
        IConsoleWriter consoleReaction,
        IConsoleOperation consoleOperation,
        INotepadUserDataStoragePort notepadUserDataFiles,
        INotepadDataStoragePort notepadDataFiles,
        INotepadUserDataPersistencePort notepadUserDataPersistence) :
        Interaction(exceptionPort, cancellationPort, consoleReaction, consoleOperation, notepadUserDataFiles, notepadDataFiles)
    {
        protected IConsoleWriter ConsoleReaction => consoleReaction;

        public async Task<FlowEndToken> ExecuteRetryLoopAsync(IFlowContext context)
        {
            while (true)
            {
                var end = await ExecuteAsync(context);
                await Write(context, "");

                if (end.HasCanceled || !end.HasException)
                {
                    return end;
                }
            }
        }

        public override async Task<FlowEndToken> ExecuteAsync(IFlowContext context)
        {
            return await TryCatchBlockAsync(context, async context =>
            {
                // セキュリティのためのメモリリセット
                notepadUserDataFiles.ForceResetMemoryState();
                notepadDataFiles.ForceResetMemoryState();

                await OnBeforeLoginAsync();

                using var scope = consoleReaction.GetStateScope();
                scope.State.Update(writeLine: true);

                string userID;
                NotepadUserKey userKey = default;
                do
                {
                    await Write(context, "# Login - Enter your id (if Empty, use public note) :");

                    userID = (await consoleOperation.WaitUserTextAsync(context)).text;

                    if (!new NotepadUserKey(userID).IsValid)
                    {
                        await Write(context, "- Invalid user id, Retry enter your id :");
                        continue;
                    }
                    else if (string.IsNullOrEmpty(userID))
                    {
                        context = new NotepadContext();
                        break;
                    }
                    else
                    {
                        userKey = new NotepadUserKey(userID);
                        var userObject = new NotepadUserObject(userKey);
                        context = new NotepadContext(userObject);
                        break;
                    }

                } while (true);

                await OnBeforeLoadingUserDataAsync(context);

                await Write(context, "> Loading User data...");
                var userDataResult = await notepadUserDataFiles.LoadUserDataAsync(notepadUserDataPersistence, context);
                if (!userDataResult)
                {
                    throw userDataResult.Exception!;
                }

                var viewName = string.IsNullOrEmpty(userID) ? "Public" : userID;
                return await Write(context, $"> Logined - {viewName} ({userDataResult.Value!.Count()} Notes)");
            });
        }

        protected virtual ValueTask OnBeforeLoginAsync()
        {
            // セキュリティのためのメモリリセット
            return default;
        }

        protected virtual ValueTask OnBeforeLoadingUserDataAsync(IFlowContext context)
        {
            return default;
        }

        private async Task<FlowEndToken> Write(IFlowContext context, string text)
        {
            return await consoleReaction.Write(context, new ConsoleOutput(text));
        }
    }
}
