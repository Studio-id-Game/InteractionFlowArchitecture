using InteractionFlow.Core.ExternalPorts.StoragePorts.Entries;
using InteractionFlow.Samples.Notepad.Core.Entities.Datas;
using InteractionFlow.Samples.Notepad.Core.Entities.Keys;

namespace InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts.Entries
{
    public class NotepadUserEntry(NotepadUserKey fileID, NotepadUserData value)
        : PersistentEntry<NotepadUserKey, NotepadUserData>(fileID, value)
    {
        public NotepadUserData NotepadUserData
        {
            get => Value!;
        }
    }
}
