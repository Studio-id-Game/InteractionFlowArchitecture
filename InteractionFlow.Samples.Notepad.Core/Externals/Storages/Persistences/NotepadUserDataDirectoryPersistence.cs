using InteractionFlow.Core.Entities;
using InteractionFlow.Samples.Notepad.Core.Entities.Datas;
using InteractionFlow.Samples.Notepad.Core.Entities.Keys;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts.PersistencePorts;
using InteractionFlow.Standard.Externals.Storages.Persistences;
using System.IO;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Core.Externals.Storages.Persistences
{

    public sealed class NotepadUserDataDirectoryPersistence(
        INotepadDataPersistencePort filePersistence,
        INotepadDataStoragePort notepadStorage)
        : DirectoryPersistence<NotepadUserKey, NotepadUserData>(filePersistence, notepadStorage), INotepadUserDataPersistencePort
    {
        public override string RootPath => Path.Combine(base.RootPath, "NotepadData");

        public override NotepadUserKey GetDirectoryId(string dirName)
        {
            if (dirName == NotepadUserKey.Public.Name)
            {
                return NotepadUserKey.Public;
            }
            else
            {
                return new(dirName);
            }
        }

        public override string GetDirectoryName(NotepadUserKey id)
        {
            return id.Name;
        }

        protected override async Task<Result<NotepadUserData>> LoadDirectory(string path, NotepadUserKey id, Result<NotepadUserData> oldValue)
        {
            return await oldValue.StartAsync()
                .ThenAsync(userData =>
                {
                    userData.Clear();
                    var fileIds = filePersistence.GetAlllIdWithUser(id);
                    foreach (var fileId in fileIds)
                    {
                        userData.Add(fileId);
                    }

                    return Task.FromResult(userData.AsResult());
                })
                .ConfigureAwait(false);
        }

        protected override async Task<Result> SaveDirectory(string path, NotepadUserKey id, Result<NotepadUserData> value)
        {
            return await value.StartAsync()
                .ThenAsync(async userData =>
                {
                    var fileIds = filePersistence.GetAlllIdWithUser(id);

                    //Delete old items
                    foreach (var fileId in fileIds)
                    {
                        if (!userData.Contains(fileId))
                        {
                            await filePersistence.Delete(fileId).ConfigureAwait(false);
                        }
                    }

                    var result = Result.Success;

                    //Save or Add items
                    foreach (var fileId in userData)
                    {
                        result = await notepadStorage.GetOrCreate(fileId).StartAsync()
                            .ThenAsync(async entry => await entry.SaveIfChanged(filePersistence).ConfigureAwait(false))
                            .ConfigureAwait(false);

                        if (!result.Try(out _))
                        {
                            break;
                        }
                    }

                    return result;
                })
                .ConfigureAwait(false);
        }
    }
}
