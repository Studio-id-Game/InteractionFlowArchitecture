using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts.ReactionPorts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Samples.Notepad.Core.Entities.Contexts;
using InteractionFlow.Samples.Notepad.Core.Entities.Keys;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts.PersistencePorts;
using InteractionFlow.Samples.Notepad.Core.Interactions.Rules;
using InteractionFlow.Standard.Console.Entities;
using InteractionFlow.Standard.Console.ExternalPorts.OperationPorts;
using InteractionFlow.Standard.Console.ExternalPorts.ReactionPorts;
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
        INotepadUserDataPersistencePort notepadUserDataPersistence,
        params IDependencyNode[] dependency) :
        Interaction(
            exceptionPort,
            cancellationPort,
            [consoleReaction,
                consoleOperation,
                notepadUserDataFiles,
                notepadDataFiles,
                notepadUserDataPersistence,
                .. dependency])
    {
        protected IConsoleWriter ConsoleReaction => consoleReaction;
        protected IConsoleOperation ConsoleOperation => consoleOperation;
        protected INotepadUserDataStoragePort NotepadUserDataFiles => notepadUserDataFiles;
        protected INotepadDataStoragePort NotepadDataFiles => notepadDataFiles;
        protected INotepadUserDataPersistencePort NotepadUserDataPersistence => notepadUserDataPersistence;

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

        protected override async Task<ReactionEnd> ExecuteCoreAsync(IFlowContext context)
        {
            var notepadContext = context as NotepadContext
                ?? throw new InvalidOperationException($"{nameof(Login)} requires {nameof(NotepadContext)}.");

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
                    notepadContext.User = NotepadUserObject.Public;
                    notepadContext.CurrentNotepadKey = NotepadDataKey.Empty;
                    break;
                }
                else
                {
                    userKey = new NotepadUserKey(userID);
                    var userObject = new NotepadUserObject(userKey);
                    notepadContext.User = userObject;
                    notepadContext.CurrentNotepadKey = NotepadDataKey.Empty;
                    break;
                }

            } while (true);

            await OnBeforeLoadingUserDataAsync(notepadContext);

            await Write(context, "> Loading User data...");
            return await notepadUserDataFiles.LoadUserDataAsync(notepadUserDataPersistence, notepadContext)
                .ResolveAsync(
                    onSuccess: async userData =>
                    {
                        var viewName = string.IsNullOrEmpty(userID) ? "Public" : userID;
                        return await Write(notepadContext, $"> Logined - {viewName} ({userData.Count()} Notes)");
                    },
                    onFailure: async e =>
                    {
                        return await Write(notepadContext, $"> Login error : {e.Message}");
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

        private async Task<ReactionEnd> Write(IFlowContext context, string text)
        {
            return await consoleReaction.Write(context, new ConsoleOutput(text));
        }
    }
}
