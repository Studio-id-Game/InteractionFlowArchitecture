using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts.ReactionPorts;
using InteractionFlow.Samples.Notepad.Core.Entities.Keys;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts.PersistencePorts;
using InteractionFlow.Samples.Notepad.Core.Interactions;
using InteractionFlow.Samples.Notepad.Secure.ExternalPorts.StoragePorts;
using InteractionFlow.Samples.Notepad.Secure.ExternalPorts.StoragePorts.PersistencePorts;
using InteractionFlow.Samples.Notepad.Secure.ExternalPorts.StoragePorts.SecureManagerPorts;
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
            INotepadUserDataStoragePort notepadUserDataFiles,
            INotepadDataStoragePort notepadDataFiles,
            INotepadUserDataPersistencePort notepadUserDataPersistence,
            //Custom
            ICurrentUserStoragePort currentUserStorage,
            IUserSecureDataPersistencePort userSecureDataPersistence,
            ISecureManagerPort secureManager,
            EnterPassword enterPassword)
        : Login(
            exceptionPort,
            cancellationPort,
            consoleReaction,
            consoleOperation,
            notepadUserDataFiles,
            notepadDataFiles,
            notepadUserDataPersistence)
    {
        protected override async ValueTask OnBeforeLoginAsync()
        {
            currentUserStorage.ForceResetMemoryState();
            await base.OnBeforeLoginAsync();
        }

        protected override async ValueTask OnBeforeLoadingUserDataAsync(IFlowContext context)
        {
            if (!context.TryGet<NotepadUserKey>(out var userkey))
            {
                throw new InvalidOperationException();
            }

            var currentUserResult = currentUserStorage.GetOrCreate(userkey);

            if (!currentUserResult)
            {
                throw currentUserResult.Exception!;
            }

            var currentUser = currentUserResult.Value!;

            var loadResult = await currentUser.Load(userSecureDataPersistence);
            if (!loadResult)
            {
                await ConsoleReaction.Write(context, new("> Create New UserFile ..."));
                currentUser.Value!.UserSalt = secureManager.GetNewUserSalt();
                var saveResult = await currentUser.Save(userSecureDataPersistence);
                if (!saveResult)
                {
                    throw saveResult.Exception!;
                }
            }

            var result = await enterPassword.ExecuteAsync(context);
            if (result.HasException)
            {
                throw result.Exception!;
            }
        }
    }
}
