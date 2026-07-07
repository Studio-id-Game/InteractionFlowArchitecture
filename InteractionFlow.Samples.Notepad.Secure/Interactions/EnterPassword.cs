using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts.ReactionPorts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Samples.Notepad.Secure.ExternalPorts.StoragePorts;
using InteractionFlow.Samples.Notepad.Secure.ExternalPorts.StoragePorts.SecureManagerPorts;
using InteractionFlow.Standard.Entities;
using InteractionFlow.Standard.Entities.Consoles;
using InteractionFlow.Standard.ExternalPorts.OperationPorts;
using InteractionFlow.Standard.ExternalPorts.ReactionPorts;
using InteractionFlow.Standard.ExternalPorts.SilentExternalPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Secure.Interactions

{
    internal class EnterPassword(
            IExceptionPort<Exception> exceptionPort,
            ICancellationPort cancellationPort,
            IConsoleWriter consoleReaction,
            IConsoleCursorPositionAccess consoleCursorPositionAccess,
            IConsoleOperation consoleOperation,
            ICurrentUserStoragePort currentUserStorage,
            ISecureManagerPort secureManager)
        : Interaction(
            exceptionPort,
            cancellationPort,
            consoleReaction,
            consoleCursorPositionAccess,
            consoleOperation,
            currentUserStorage)
    {
        public override Task<FlowEndToken> ExecuteAsync(IFlowContext context)
        {
            return TryCatchBlockAsync(context, async (context) =>
            {
                using var consoleReactionScope = consoleReaction.GetStateScope();
                using var consoleOperationScope = consoleOperation.GetStateScope();
                consoleReactionScope.State.Update(writeLine: true);
                consoleOperationScope.State.Update(writeLine: true);

                return await currentUserStorage.GetKey(context).StartAsync()
                    .ThenAsync(async userKey =>
                    {
                        return currentUserStorage.GetOrCreate(userKey);
                    })
                    .ThenAsync(async currentUser =>
                    {
                        if (currentUser.Value == null)
                        {
                            return new NullReferenceException("currentUser.Value == null");
                        }
                        var pass = await EnterPassAsync(context, consoleReactionScope);
                        secureManager.GetUserKey(pass, currentUser.Value);
                        consoleReactionScope.State.Update(writeLine: true);
                        return Result.Success;
                    })
                    .ResolveAsync(
                    onSuccess: async () =>
                    {
                        return await consoleReaction.Write(context, new("> Password Entered."));
                    },
                    onFailure: async e =>
                    {
                        return await consoleReaction.Write(context, new($"> Password Error : {e.Message}"));
                    });
            });
        }

        private async Task<string> EnterPassAsync(IFlowContext context, FunctionStateScope<ConsoleState> consoleReactionScope)
        {
            consoleReactionScope.State.Update(writeLine: true);
            await consoleReaction.Write(context, new("Enter Password: "));

            var pass = "";
            consoleOperation.State.Update(writeLine: false);
            consoleReactionScope.State.Update(writeLine: false);

            while (true)
            {
                var key = (await consoleOperation.WaitUserKeyAsync(context, true)).key;
                if (key.Key == ConsoleKey.Enter)
                {
                    break;
                }

                var keyChar = key.KeyChar;

                if (key.Key == ConsoleKey.Backspace)
                {
                    if (pass.Length > 0)
                    {
                        var pos = consoleCursorPositionAccess.Position;
                        consoleCursorPositionAccess.Position = new(null, pos.Left - 1);
                        await consoleReaction.Write(context, new(" "));
                        consoleCursorPositionAccess.Position = new(null, pos.Left - 1);
                        pass = pass[..^1];
                    }
                }
                else if (keyChar != '\0')
                {
                    pass += keyChar;
                    await consoleReaction.Write(context, new("*"));
                }
            }

            consoleOperation.State.Update(writeLine: true);
            consoleReactionScope.State.Update(writeLine: true);
            await consoleReaction.Write(context, new(""));

            return pass;
        }
    }
}
