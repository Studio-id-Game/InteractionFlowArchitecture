using InteractionFlow.Samples.Notepad.Entities.Datas;
using InteractionFlow.Standard.StoragePorts;

namespace InteractionFlow.Samples.Notepad.StoragePorts
{
    internal interface INotepadDataMemory : IStoragePortModifiable<NotepadData>
    {
    }
}
