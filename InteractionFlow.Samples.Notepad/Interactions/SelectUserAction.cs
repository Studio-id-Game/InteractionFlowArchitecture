using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Core.ReactionPorts;
using InteractionFlow.Samples.Notepad.Interactions.Rules;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.OperationPorts;
using InteractionFlow.Standard.ReactionPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Interactions
{
    internal class SelectUserAction(
        IExceptionPort exceptionPort,
        ICancellationPort cancellationPort,
        IConsoleReaction consoleReaction,
        IConsoleOperation consoleOperation,
        NoteCreate noteCreate,
        NoteDelete noteDelete,
        NoteEdit noteEdit,
        Login login) :
        Interaction(exceptionPort, cancellationPort)
    {
        private readonly ConsoleSelectItem<IInteraction> userActions = new(consoleReaction, consoleOperation, new()
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

        public override async Task<FlowEndToken> InteractWithUserAsync(IFlowContext context)
        {
            return await TryCatchBlock(context, async context =>
            {
                await Write(context, "# Select your action :");

                var (select, action) = await userActions.GetSelectAsync(context);

                await Write(context, $"> UserAction - {select}");

                return await action.InteractWithUserAsync(context);
            });
        }

        private async Task Write(IFlowContext context, string text)
        {
            await consoleReaction.ReactToUserAsync(context, new ConsoleOutput(text));
        }
    }
}
