using InteractionFlow.Samples.Notepad.Core.Entities.Keys;
using InteractionFlow.Samples.Notepad.Secure.Entities;
using InteractionFlow.Standard.FileSystem.ExternalPorts.StoragePorts.PersistencePorts;

namespace InteractionFlow.Samples.Notepad.Secure.ExternalPorts.StoragePorts.PersistencePorts
{
    public interface IUserSecureDataPersistencePort : IFilePersistencePort<NotepadUserKey, UserSecureData>
    {

    }
}
