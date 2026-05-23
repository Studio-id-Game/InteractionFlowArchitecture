using InteractionFlow.Samples.Notepad.Entities.Datas;
using InteractionFlow.Standard.ExternalPorts.StoragePorts;

namespace InteractionFlow.Samples.Notepad.ExternalPorts.StoragePorts
{
    internal interface INotepadDataFiles : INotepadDataMemory, IExternalStoragePortModifiable<NotepadData>
    {

    }
}
