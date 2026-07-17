using InteractionFlow.Core.ExternalPorts.StoragePorts;
using InteractionFlow.Core.ExternalPorts.StoragePorts.Entries;
using InteractionFlow.Samples.Notepad.Core.Entities.Keys;
using InteractionFlow.Samples.Notepad.Secure.Entities;

namespace InteractionFlow.Samples.Notepad.Secure.ExternalPorts.StoragePorts
{
    public interface ICurrentUserStoragePort : IStoragePort<NotepadUserKey, PersistentEntry<NotepadUserKey, UserSecureData>>
    {
        NotepadUserKey LastUser { get; }
    }
}
