using InteractionFlow.Samples.Notepad.Core.Entities.Datas;
using InteractionFlow.Samples.Notepad.Core.Entities.Keys;

namespace InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts.PersistencePorts
{
    public interface INotepadDataPersistencePort : IPersistencePort<NotepadDataKey, NotepadData>
    {
        public string GetViewName(NotepadDataKey key);

        public NotepadDataKey[] GetAlllIdWithUser(NotepadUserKey notepadUserKey);
    }
}
