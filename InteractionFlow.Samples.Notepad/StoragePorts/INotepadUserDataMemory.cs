using InteractionFlow.Core.StoragePorts;
using InteractionFlow.Samples.Notepad.Entities.Datas;
using InteractionFlow.Samples.Notepad.Entities.Keys;

namespace InteractionFlow.Samples.Notepad.StoragePorts
{
    internal interface INotepadUserDataMemory : IStoragePortModifiable<NotepadUserData>
    {
        public bool Exist(NotepadDataKey dataKey);
    }
}
