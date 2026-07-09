using InteractionFlow.Core.ExternalPorts.StoragePorts;
using InteractionFlow.Samples.Notepad.Core.Entities.Keys;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts.Entries;

namespace InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts
{
    public interface INotepadUserDataStoragePort : IStoragePort<NotepadUserKey, NotepadUserEntry>
    {
    }
}
