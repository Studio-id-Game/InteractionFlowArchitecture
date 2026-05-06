using InteractionFlow.Samples.Notepad.Entities.Datas;
using InteractionFlow.Samples.Notepad.Entities.Keys;
using InteractionFlow.Samples.Notepad.StoragePorts;
using InteractionFlow.Standard.Storages;

namespace InteractionFlow.Samples.Notepad.Storages
{
    internal class NotepadDataMemory : KeyedMemoryModifiable<NotepadDataKey, NotepadData>, INotepadDataMemory
    {

    }
}
