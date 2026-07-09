using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Entities.Contexts;
using InteractionFlow.Samples.Notepad.Core.Entities.Datas;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts.Entries;
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
                .ThenAsync(key =>
                {
                    return Task.FromResult(notepadUserDataFiles.GetOrCreate(key));
                })
                .ThenErrorAsync(e =>
                {
                    return Task.FromResult<Result<NotepadUserEntry>>(e);
                })
                .ThenAsync(async userData =>
                {
                    return await userData.Load(notepadUserDataPersistence).ConfigureAwait(false);
                })
                .ConfigureAwait(false);
        }
    }
}
