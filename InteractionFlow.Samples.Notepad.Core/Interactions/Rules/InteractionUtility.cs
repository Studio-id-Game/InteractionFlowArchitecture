using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Samples.Notepad.Core.Entities.Datas;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts.PersistencePorts;
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
            return await notepadUserDataFiles.GetKey(context).StartAsync()
                .ThenAsync(async key =>
                {
                    return notepadUserDataFiles.GetOrCreate(key);
                })
                .ThenErrorAsync(async e =>
                {
                    return e;
                })
                .ThenAsync(async userData =>
                {
                    return await userData.Load(notepadUserDataPersistence);
                });
        }
    }
}
