using InteractionFlow.Samples.Notepad.Entities.Datas;
using InteractionFlow.Samples.Notepad.Entities.Keys;
using InteractionFlow.Standard.ExternalPorts.StoragePorts;

namespace InteractionFlow.Samples.Notepad.ExternalPorts.StoragePorts
{
    internal interface INotepadUserDataMemory : IStoragePortModifiable<NotepadUserData>
    {
        public bool Exist(NotepadDataKey dataKey);
    }
}
