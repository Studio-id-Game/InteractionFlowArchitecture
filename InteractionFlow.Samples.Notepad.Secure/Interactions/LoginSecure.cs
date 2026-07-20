using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Core.ExternalPorts.ReactionPorts;
using InteractionFlow.Core.ExternalPorts.StoragePorts.Entries;
using InteractionFlow.Samples.Notepad.Core.Entities.Keys;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts.PersistencePorts;
using InteractionFlow.Samples.Notepad.Core.Interactions;
using InteractionFlow.Samples.Notepad.Core.Interactions.Rules;
using InteractionFlow.Samples.Notepad.Secure.Entities;
using InteractionFlow.Samples.Notepad.Secure.ExternalPorts.StoragePorts;
using InteractionFlow.Samples.Notepad.Secure.ExternalPorts.StoragePorts.PersistencePorts;
using InteractionFlow.Samples.Notepad.Secure.ExternalPorts.StoragePorts.SecureManagerPorts;
using InteractionFlow.Standard.Console.ExternalPorts.OperationPorts;
using InteractionFlow.Standard.Console.ExternalPorts.ReactionPorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Secure.Interactions
{

    internal sealed class LoginSecure(
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
            INotepadDataPersistencePort notepadDataPersistence,
            EnterPassword enterPassword)
        : Login(
            exceptionPort,
            cancellationPort,
            consoleReaction,
            consoleOperation,
            notepadUserDataFiles,
            notepadDataFiles,
            notepadUserDataPersistence,
            currentUserStorage,
            userSecureDataPersistence,
            notepadDataPersistence,
            enterPassword)
    {
        private const string ExceptionDataKey_CurrentUserEntry = "currentUserEntry";

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

            await currentUserStorage.GetOrCreate(userkey).StartAsync()
                .ThenAsync(async currentUserEntry =>
                {
                    PersistentEntry<NotepadUserKey, UserSecureData> cv = currentUserEntry;
                    return await currentUserEntry.Load(userSecureDataPersistence)
                    .ThenErrorAsync(e =>
                    {
                        e.Data[ExceptionDataKey_CurrentUserEntry] = currentUserEntry;
                        return Task.FromResult<Result<UserSecureData>>(e);
                    });
                })
                .ThenAsync(e =>
                {
                    return Task.FromResult(Result.Success);
                })
                .ThenErrorAsync(async e =>
                {
                    if (e.Data.Contains(ExceptionDataKey_CurrentUserEntry) &&
                        e.Data[ExceptionDataKey_CurrentUserEntry] is PersistentEntry<NotepadUserKey, UserSecureData> currentUserEntry)
                    {
                        await ConsoleReaction.Write(context, new("> Create New UserFile ..."));
                        currentUserEntry.Value!.UserSalt = secureManager.GetNewUserSalt();
                        return await currentUserEntry.Save(userSecureDataPersistence)
                            .ThenAsync(async () =>
                            {
                                await ConsoleReaction.Write(context, new("> UserFile Saved."));
                                return Result.Success;
                            })
                            .ThenErrorAsync(async e =>
                            {
                                await ConsoleReaction.Write(context, new("> UserFile Save Error."));
                                return e;
                            });
                    }
                    else
                    {
                        return e;
                    }
                })
                .ResolveAsync(
                onSuccess: async () =>
                {
                    await ConsoleReaction.Write(context, new("> Enter User key ..."));
                    await enterPassword.ExecuteAsync(context);
                    await NotepadUserDataFiles.LoadUserDataAsync(NotepadUserDataPersistence, context)
                        .ThenAsync(async userData =>
                        {
                            foreach (var noteDataKey in userData)
                            {
                                var result = await NotepadDataFiles.GetOrCreate(noteDataKey).StartAsync()
                                   .ThenAsync(async notepadEntry =>
                                   {
                                       return await notepadEntry.Load(notepadDataPersistence);
                                   })
                                   .ThenAsync(notepadData =>
                                   {
                                       return Task.FromResult(NotepadDataFiles.RemoveWithoutDispose(noteDataKey));
                                   });

                                if (!result.Try(out var e))
                                {
                                    await ConsoleReaction.Write(context, new($"> Critical Login Error!"));
                                    Environment.Exit(-1);
                                }
                            }

                            return Result.Success;
                        });

                    return await ConsoleReaction.Write(context, new($"> Logined : {userkey.Name}"));
                },
                onFailure: async e =>
                {
                    return await ConsoleReaction.Write(context, new($"> Login Error : {e.Message}"));
                });
        }
    }
}
