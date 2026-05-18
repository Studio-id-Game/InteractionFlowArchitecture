using InteractionFlow.Samples.Notepad.Entities.Datas;
using InteractionFlow.Samples.Notepad.Entities.Keys;
using InteractionFlow.Standard.StoragePorts;

namespace InteractionFlow.Samples.Notepad.StoragePorts
{
    internal interface INotepadUserDataMemory : IStoragePortModifiable<NotepadUserData>
    {
        public bool Exist(NotepadDataKey dataKey);
    }
}
