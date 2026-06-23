using InteractionFlow.Samples.Notepad.Core.Entities.Keys;
using InteractionFlow.Samples.Notepad.Secure.Entities.Datas;
using InteractionFlow.Samples.Notepad.Secure.ExternalPorts.StoragePorts;
using InteractionFlow.Standard.Externals.Storages;

namespace InteractionFlow.Samples.Notepad.Secure.Externals.Storages
{
    internal class NotepadUserSecureDataMemory : KeyedMemoryModifiable<NotepadUserKey, NotepadUserSecureData>, INotepadUserSecureDataMemory
    {
    }
}
