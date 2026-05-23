using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts.ReactionPorts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Samples.Notepad.Entities.Contexts;
using InteractionFlow.Samples.Notepad.Entities.Keys;
using InteractionFlow.Samples.Notepad.ExternalPorts.StoragePorts;
using InteractionFlow.Standard.Entities;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.ExternalPorts.OperationPorts;
using InteractionFlow.Standard.ExternalPorts.ReactionPorts;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Interactions
{

    internal class Login(
        IExceptionPort<Exception> exceptionPort,
        ICancellationPort cancellationPort,
        IConsoleWriter consoleReaction,
        IConsoleOperation consoleOperation,
        INotepadUserDataFiles notepadUserDataFiles) :
        Interaction(exceptionPort, cancellationPort, consoleReaction, consoleOperation, notepadUserDataFiles)
    {
        public override async Task<FlowEndToken> ExecuteAsync(IFlowContext context)
        {
            return await TryCatchBlockAsync(context, async context =>
            {
                using var scope = consoleReaction.GetStateScope();
                scope.State.Update(writeLine: true);

                string userID;
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
                        context = new NotepadContext(new(new(userID)));
                        break;
                    }

                } while (true);

                var load = notepadUserDataFiles.LoadFromPersistentAsync(context);
                await Write(context, "> Loading User dada...");
                var result = await load;

                var viewName = string.IsNullOrEmpty(userID) ? "Public" : userID;
                return await Write(context, $"> Logined - {viewName} ({result.Value!.Count()} Notes)");
            });
        }

        private async Task<FlowEndToken> Write(IFlowContext context, string text)
        {
            return await consoleReaction.Write(context, new ConsoleOutput(text));
        }
    }
}
