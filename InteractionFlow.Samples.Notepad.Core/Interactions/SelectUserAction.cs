using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts.ReactionPorts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts;
using InteractionFlow.Samples.Notepad.Core.Interactions.Rules;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.ExternalPorts.OperationPorts;
using InteractionFlow.Standard.ExternalPorts.ReactionPorts;
using InteractionFlow.Standard.ExternalPorts.SilentExternalPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Core.Interactions
{
    public class SelectUserAction(
        IExceptionPort<Exception> exceptionPort,
        ICancellationPort cancellationPort,
        IConsoleWriter consoleReaction,
        IConsoleCursorPositionAccess consoleCursorPositionAccess,
        IConsoleOperation consoleOperation,
        INotepadDataStoragePort notepadDataFiles,
        NoteCreate noteCreate,
        NoteDelete noteDelete,
        NoteEdit noteEdit,
        Login login) :
        Interaction(exceptionPort, cancellationPort, consoleReaction, consoleCursorPositionAccess, consoleOperation, notepadDataFiles, noteCreate, noteDelete, noteEdit, login)
    {
        private readonly ConsoleSelectItem<IInteraction> userActions = new(consoleReaction, consoleCursorPositionAccess, consoleOperation, new()
        {
            ["1. Edit Exist Note"] = noteEdit,
            ["2. Create New Note"] = noteCreate,
            ["3. Delete Old Note"] = noteDelete,
            ["4. Login As Other User"] = login,
        });

        static SelectUserAction()
        {
            Console.CursorLeft = 0;
            Console.CursorTop = 0;
        }

        protected override async Task<ReactionEnd> ExecuteCoreAsync(IFlowContext context)
        {
            try
            {
                using var scope = consoleReaction.GetStateScope();
                scope.State.Update(writeLine: true);

                await Write(context, "# Select your action :");

                var (select, action) = await userActions.GetSelectAsync(context);

                await Write(context, $"> UserAction - {select}");

                FlowEndToken end;
                if (action is Login login)
                {
                    end = await login.ExecuteRetryLoopAsync(context);
                }
                else
                {
                    end = await action.ExecuteAsync(context);
                }

                return end.End;
            }
            finally
            {
                notepadDataFiles.ClearWithoutDispose();
            }
        }

        private async Task Write(IFlowContext context, string text)
        {
            await consoleReaction.Write(context, new ConsoleOutput(text));
        }
    }
}
