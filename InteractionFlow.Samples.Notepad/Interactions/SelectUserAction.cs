using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts.ReactionPorts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Samples.Notepad.ExternalPorts.StoragePorts;
using InteractionFlow.Samples.Notepad.Interactions.Rules;
using InteractionFlow.Standard.Entities;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.ExternalPorts.OperationPorts;
using InteractionFlow.Standard.ExternalPorts.ReactionPorts;
using InteractionFlow.Standard.ExternalPorts.SilentPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Interactions
{
    internal class SelectUserAction(
        IExceptionPort<Exception> exceptionPort,
        ICancellationPort cancellationPort,
        IConsoleWriter consoleReaction,
        IConsoleCursorPositionAccess consoleCursorPositionAccess,
        IConsoleOperation consoleOperation,
        INotepadDataFiles notepadDataFiles,
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

        public override async Task<FlowEndToken> ExecuteAsync(IFlowContext context)
        {
            return await TryCatchBlockAsync(context, async context =>
            {
                try
                {
                    using var scope = consoleReaction.GetStateScope();
                    scope.State.Update(writeLine: true);

                    await Write(context, "# Select your action :");

                    var (select, action) = await userActions.GetSelectAsync(context);

                    await Write(context, $"> UserAction - {select}");

                    return await action.ExecuteAsync(context);
                }
                finally
                {
                    notepadDataFiles.Clear();
                }
            });
        }

        private async Task Write(IFlowContext context, string text)
        {
            await consoleReaction.Write(context, new ConsoleOutput(text));
        }
    }
}
