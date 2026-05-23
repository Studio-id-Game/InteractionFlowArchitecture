using InteractionFlow.Samples.Notepad.Entities.Datas;
using InteractionFlow.Samples.Notepad.Entities.Keys;
using InteractionFlow.Samples.Notepad.ExternalPorts.StoragePorts;
using InteractionFlow.Standard.Externals.Storages;

namespace InteractionFlow.Samples.Notepad.Externals.Storages
{
    internal class NotepadDataMemory : KeyedMemoryModifiable<NotepadDataKey, NotepadData>, INotepadDataMemory
    {

    }
}
