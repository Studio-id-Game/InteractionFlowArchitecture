using InteractionFlow.Core.ExternalPorts.StoragePorts;
using InteractionFlow.Samples.Notepad.Core.Entities.Keys;
using InteractionFlow.Samples.Notepad.Secure.Entities;
using InteractionFlow.Standard.ExternalPorts.StoragePorts.Entries;

namespace InteractionFlow.Samples.Notepad.Secure.ExternalPorts.StoragePorts
{
    public interface ICurrentUserStoragePort : IStoragePort<NotepadUserKey, PersistentEntry<NotepadUserKey, UserSecureData>>
    {
        NotepadUserKey LastUser { get; }
    }
}
