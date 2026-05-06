using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Core.ReactionPorts;
using InteractionFlow.Samples.Notepad.Entities.Contexts;
using InteractionFlow.Samples.Notepad.Entities.Keys;
using InteractionFlow.Samples.Notepad.StoragePorts;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.OperationPorts;
using InteractionFlow.Standard.ReactionPorts;
using System.Linq;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Interactions
{

    internal class Login(
        IExceptionPort exceptionPort,
        ICancellationPort cancellationPort,
        IConsoleReaction consoleReaction,
        IConsoleOperation consoleOperation,
        INotepadUserDataFiles notepadUserDataFiles) :
        Interaction(exceptionPort, cancellationPort)
    {
        public override async Task<FlowEndToken> InteractWithUserAsync(IFlowContext context)
        {
            return await TryCatchBlock(context, async context =>
            {
                string userID;
                do
                {
                    await consoleReaction.ReactToUserAsync(context, new ConsoleOutput("# Login - Enter your id (if Empty, use public note) :"));

                    userID = (await consoleOperation.UserOperateTextAsync(context)).text;

                    if (!new NotepadUserKey(userID).IsValid)
                    {
                        await consoleReaction.ReactToUserAsync(context, new ConsoleOutput("- Invalid user id, Retry enter your id :"));
                        continue;
                    }
                    else if (string.IsNullOrEmpty(userID))
                    {
                        context = new NotepadContext();
                        break;
                    }
                    else
                    {
                        context = new NotepadContext(new(new(userID)));
                        break;
                    }

                } while (true);

                var load = notepadUserDataFiles.LoadFromPersistent(context);
                await consoleReaction.ReactToUserAsync(context, new ConsoleOutput("> Loading User dada..."));
                var result = await load;

                var viewName = string.IsNullOrEmpty(userID) ? "Public" : userID;
                return await EndInteractAsync(context, consoleReaction, new ConsoleOutput($"> Logined - {viewName} ({result.Value!.Count()} Notes)"));
            });
        }
    }
}
