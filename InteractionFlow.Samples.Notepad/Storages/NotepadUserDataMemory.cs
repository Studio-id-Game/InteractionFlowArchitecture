using InteractionFlow.Samples.Notepad.Entities.Datas;
using InteractionFlow.Samples.Notepad.Entities.Keys;
using InteractionFlow.Samples.Notepad.StoragePorts;
using InteractionFlow.Standard.Storages;
using System.Linq;

namespace InteractionFlow.Samples.Notepad.Storages
{
    internal class NotepadUserDataMemory : KeyedMemoryModifiable<NotepadUserKey, NotepadUserData>, INotepadUserDataMemory
    {
        public bool Exist(NotepadDataKey dataKey)
        {
            if (!dataKey.IsValid) return false;
            var userKey = new NotepadUserKey(dataKey.UserId);
            return Dictionary[userKey].Contains(dataKey);
        }
    }
}
