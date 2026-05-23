using InteractionFlow.Samples.Notepad.Core.Entities.Datas;
using InteractionFlow.Standard.ExternalPorts.StoragePorts;

namespace InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts
{
    public interface INotepadDataMemory : IStoragePortModifiable<NotepadData>
    {
        public void Clear();
    }
}
