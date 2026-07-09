using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Externals.Storages;
using InteractionFlow.Samples.Notepad.Core.Entities.Datas;
using InteractionFlow.Samples.Notepad.Core.Entities.Keys;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts.Entries;

namespace InteractionFlow.Samples.Notepad.Core.Externals.Storages
{
    public class NotepadDataStorage : Storage<NotepadDataKey, NotepadEntry>, INotepadDataStoragePort
    {
        protected override Result CanRemoveValue(NotepadDataKey key, NotepadEntry value)
        {
            return Result.Success;
        }

        protected override Result<NotepadEntry> CreateNewValue(NotepadDataKey key)
        {
            var data = new NotepadData(key)
            {
                Title = $"New Title - {key.UserId}.{key.NoteId}",
                Text = "New Text",

            };
            var entry = new NotepadEntry(key, data);
            return entry;
        }
    }
}
