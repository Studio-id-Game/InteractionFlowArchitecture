using InteractionFlow.Samples.Notepad.Entities.Datas;
using InteractionFlow.Samples.Notepad.Entities.Keys;
using InteractionFlow.Samples.Notepad.ExternalPorts.StoragePorts;
using InteractionFlow.Standard.Externals.Storages;
using System.Linq;

namespace InteractionFlow.Samples.Notepad.Externals.Storages
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
