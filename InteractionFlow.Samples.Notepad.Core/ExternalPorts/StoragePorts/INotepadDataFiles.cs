using InteractionFlow.Samples.Notepad.Core.Entities.Datas;
using InteractionFlow.Standard.ExternalPorts.StoragePorts;

namespace InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts
{
    public interface INotepadDataFiles : INotepadDataMemory, IExternalStoragePortModifiable<NotepadData>
    {

    }
}
