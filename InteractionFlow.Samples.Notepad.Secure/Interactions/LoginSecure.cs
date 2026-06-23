using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts.ReactionPorts;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts;
using InteractionFlow.Samples.Notepad.Core.Interactions;
using InteractionFlow.Samples.Notepad.Secure.ExternalPorts.StoragePorts;
using InteractionFlow.Standard.ExternalPorts.OperationPorts;
using InteractionFlow.Standard.ExternalPorts.ReactionPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Secure.Interactions
{
    internal class LoginSecure(
            IExceptionPort<Exception> exceptionPort,
            ICancellationPort cancellationPort,
            IConsoleWriter consoleReaction,
            IConsoleOperation consoleOperation,
            INotepadUserDataFiles notepadUserDataFiles,
            INotepadDataFiles notepadDataFiles,
            INotepadUserSecureDataFiles notepadUserSecureDataFiles,
            EnterPassword enterPassword)
        : Login(
            exceptionPort,
            cancellationPort,
            consoleReaction,
            consoleOperation,
            notepadUserDataFiles,
            notepadDataFiles)
    {
        protected override async ValueTask OnBeforeLoginAsync()
        {
            await base.OnBeforeLoginAsync();
            notepadUserSecureDataFiles.ForceResetMemoryState();
        }

        protected override async ValueTask OnBeforeLoadingUserDataAsync(IFlowContext context)
        {
            var result = await enterPassword.ExecuteAsync(context);

            if (result.HasException)
            {
                throw result.Exception!;
            }
        }

    }
}
