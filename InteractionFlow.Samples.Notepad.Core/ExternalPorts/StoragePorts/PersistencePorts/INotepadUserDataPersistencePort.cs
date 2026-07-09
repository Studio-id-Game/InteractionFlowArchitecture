using InteractionFlow.Samples.Notepad.Core.Entities.Datas;
using InteractionFlow.Samples.Notepad.Core.Entities.Keys;
using InteractionFlow.Standard.ExternalPorts.StoragePorts.PersistencePorts;

namespace InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts.PersistencePorts
{
    public interface INotepadUserDataPersistencePort : IPersistencePort<NotepadUserKey, NotepadUserData>
    {

    }
}
