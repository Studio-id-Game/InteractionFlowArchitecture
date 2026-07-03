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

    public class NotepadUserDataDirectoryPersistence(
        INotepadDataPersistencePort filePersistence,
        INotepadDataStoragePort notepadStorage)
        : DirectoryPersistence<NotepadUserKey, NotepadUserData>, INotepadUserDataPersistencePort
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
            if (!oldValue)
            {
                return oldValue.Exception!;
            }

            var data = oldValue.Value!;

            data.Clear();

            var fileIds = filePersistence.GetAlllIdWithUser(id);

            foreach (var fileId in fileIds)
            {
                data.Add(fileId);
            }

            return oldValue;
        }

        protected override async Task<Result> SaveDirectory(string path, NotepadUserKey id, Result<NotepadUserData> value)
        {
            if (!value)
            {
                return value.Exception!;
            }

            var data = value.Value!;

            var fileIds = filePersistence.GetAlllIdWithUser(id);

            //Delete old items
            foreach (var fileId in fileIds)
            {
                if (!data.Contains(fileId))
                {
                    await filePersistence.Delete(fileId);
                }
            }

            //Save or Add items
            foreach (var fileId in data)
            {
                var entry = notepadStorage.GetOrCreate(fileId);
                if (!entry)
                {
                    return entry.Exception!;
                }

                var save = await entry.Value!.SaveIfChanged(filePersistence);
                if (!save)
                {
                    return save.Exception!;
                }
            }

            return true;
        }
    }
}
