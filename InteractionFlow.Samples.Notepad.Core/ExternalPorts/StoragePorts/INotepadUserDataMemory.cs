using InteractionFlow.Samples.Notepad.Core.Entities.Datas;
using InteractionFlow.Samples.Notepad.Core.Entities.Keys;
using InteractionFlow.Standard.ExternalPorts.StoragePorts;

namespace InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts
{
    public interface INotepadUserDataMemory : IStoragePortModifiable<NotepadUserData>
    {
        public bool Exist(NotepadDataKey dataKey);
    }
}
