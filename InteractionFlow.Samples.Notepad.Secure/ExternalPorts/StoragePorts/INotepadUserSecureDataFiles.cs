using InteractionFlow.Samples.Notepad.Secure.Entities.Datas;
using InteractionFlow.Standard.ExternalPorts.StoragePorts;

namespace InteractionFlow.Samples.Notepad.Secure.ExternalPorts.StoragePorts
{
    interface INotepadUserSecureDataFiles : IExternalStoragePortModifiable<NotepadUserSecureData>, INotepadUserSecureDataMemory
    {

    }

    interface INotepadUserSecureDataMemory : IStoragePortModifiable<NotepadUserSecureData>
    {

    }
}
