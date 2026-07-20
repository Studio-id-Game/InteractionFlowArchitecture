using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Architectures;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts.ReactionPorts;
using InteractionFlow.Core.Interactions;
using InteractionFlow.Samples.Notepad.Secure.ExternalPorts.StoragePorts;
using InteractionFlow.Samples.Notepad.Secure.ExternalPorts.StoragePorts.SecureManagerPorts;
using InteractionFlow.Standard.Console.ExternalPorts.OperationPorts;
using InteractionFlow.Standard.Console.ExternalPorts.ReactionPorts;
using InteractionFlow.Standard.Console.ExternalPorts.SilentExternalPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Secure.Interactions

{
    internal sealed class EnterPassword(
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
        protected override async Task<ReactionEnd> ExecuteCoreAsync(IFlowContext context)
        {
            using var consoleReactionScope = consoleReaction.GetStateScope();
            using var consoleOperationScope = consoleOperation.GetStateScope();
            consoleReactionScope.State.Update(writeLine: true);
            consoleOperationScope.State.Update(writeLine: true);

            return await currentUserStorage.GetKey(context).StartAsync()
                .ThenAsync(userKey =>
                {
                    return Task.FromResult(currentUserStorage.GetOrCreate(userKey));
                })
                .ThenAsync(async currentUser =>
                {
                    if (currentUser.Value == null)
                    {
                        return new NullReferenceException("currentUser.Value == null");
                    }
                    var pass = await EnterPassAsync(context);
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
        }

        private async Task<string> EnterPassAsync(IFlowContext context)
        {
            using var consoleReactionScope = consoleReaction.GetStateScope();
            using var consoleOperationScope = consoleReaction.GetStateScope();

            consoleReaction.State.Update(writeLine: true);
            await consoleReaction.Write(context, new("Enter Password: "));

            var pass = "";

            consoleOperation.State.Update(writeLine: false, foregroundColor: ConsoleColor.Green);
            consoleReaction.State.Update(writeLine: false, foregroundColor: ConsoleColor.Green);
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

            consoleReaction.State.Update(writeLine: true);
            await consoleReaction.Write(context, new(""));

            return pass;
        }
    }
}
