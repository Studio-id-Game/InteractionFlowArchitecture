using InteractionFlow.Core.StoragePorts;
using InteractionFlow.Samples.Notepad.Entities.Datas;

namespace InteractionFlow.Samples.Notepad.StoragePorts
{
    internal interface INotepadUserDataFiles : INotepadUserDataMemory, IStoragePortExternalModifiable<NotepadUserData>
    {

    }
}
