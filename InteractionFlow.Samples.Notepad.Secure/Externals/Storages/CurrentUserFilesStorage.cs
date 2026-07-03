using InteractionFlow.Core.Entities;
using InteractionFlow.Core.Externals.Storages;
using InteractionFlow.Samples.Notepad.Core.Entities.Keys;
using InteractionFlow.Samples.Notepad.Secure.Entities;
using InteractionFlow.Samples.Notepad.Secure.ExternalPorts.StoragePorts;
using InteractionFlow.Standard.ExternalPorts.StoragePorts.Entries;

namespace InteractionFlow.Samples.Notepad.Secure.Externals.Storages
{
    public class CurrentUserFilesStorage : Storage<NotepadUserKey, PersistentEntry<NotepadUserKey, UserSecureData>>, ICurrentUserStoragePort
    {
        public NotepadUserKey LastUser { get; private set; }

        protected override Result CanRemoveValue(NotepadUserKey key, PersistentEntry<NotepadUserKey, UserSecureData> value)
        {
            return true;
        }

        protected override Result<PersistentEntry<NotepadUserKey, UserSecureData>> CreateNewValue(NotepadUserKey key)
        {
            LastUser = key;
            return new PersistentEntry<NotepadUserKey, UserSecureData>(key, new());
        }

        public override void ForceResetMemoryState()
        {
            LastUser = default;
            base.ForceResetMemoryState();
        }
    }
}
