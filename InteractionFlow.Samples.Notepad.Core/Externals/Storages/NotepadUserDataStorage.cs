using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Externals.Storages;
using InteractionFlow.Samples.Notepad.Core.Entities.Datas;
using InteractionFlow.Samples.Notepad.Core.Entities.Keys;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts.Entries;

namespace InteractionFlow.Samples.Notepad.Core.Externals.Storages
{
    public class NotepadUserDataStorage : Storage<NotepadUserKey, NotepadUserEntry>, INotepadUserDataStoragePort
    {
        protected override Result CanRemoveValue(NotepadUserKey key, NotepadUserEntry value)
        {
            return Result.Success;
        }

        protected override Result<NotepadUserEntry> CreateNewValue(NotepadUserKey key)
        {
            var data = new NotepadUserData(key);
            var entry = new NotepadUserEntry(key, data);
            return entry;
        }
    }
}
