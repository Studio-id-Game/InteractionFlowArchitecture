using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Samples.Notepad.Core.Entities.Datas;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts.PersistencePorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Core.Interactions.Rules
{
    public static class InteractionUtility
    {
        public static async Task<Result<NotepadUserData>> LoadUserDataAsync(
            this INotepadUserDataStoragePort notepadUserDataFiles,
            INotepadUserDataPersistencePort notepadUserDataPersistence,
            IFlowContext context)
        {
            var userKeyResult = notepadUserDataFiles.GetKey(context);
            if (!userKeyResult)
            {
                return new InvalidOperationException("> Can not get user key.");
            }
            var userKey = userKeyResult.Value!;

            var userDataResult = notepadUserDataFiles.GetOrCreate(userKey);
            if (!userDataResult)
            {
                return new InvalidOperationException("> Can not get user data.");
            }
            var userData = userDataResult.Value!;

            var userDataLoadResult = await userData.Load(notepadUserDataPersistence);
            if (!userDataLoadResult)
            {
                return new InvalidOperationException("> Can not load file.");
            }

            return userData.NotepadUserData;
        }
    }
}
