using InteractionFlow.Core.StoragePorts;
using InteractionFlow.Samples.Notepad.Entities.Datas;

namespace InteractionFlow.Samples.Notepad.StoragePorts
{
    internal interface INotepadDataMemory : IStoragePortModifiable<NotepadData>
    {
    }
}
