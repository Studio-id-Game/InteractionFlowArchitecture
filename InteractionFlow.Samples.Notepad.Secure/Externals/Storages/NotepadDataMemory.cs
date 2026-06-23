using InteractionFlow.Samples.Notepad.Core.Entities.Datas;
using InteractionFlow.Samples.Notepad.Core.Entities.Keys;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts;
using InteractionFlow.Standard.Externals.Storages;

namespace InteractionFlow.Samples.Notepad.Secure.Externals.Storages
{
    internal class NotepadDataMemory : KeyedMemoryModifiable<NotepadDataKey, NotepadData>, INotepadDataMemory
    {
        public void Clear()
        {
            Dictionary.Clear();
        }
    }
}
