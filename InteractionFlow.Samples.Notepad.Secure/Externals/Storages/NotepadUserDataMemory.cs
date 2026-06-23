using InteractionFlow.Samples.Notepad.Core.Entities.Datas;
using InteractionFlow.Samples.Notepad.Core.Entities.Keys;
using InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts;
using InteractionFlow.Standard.Externals.Storages;
using System.Linq;

namespace InteractionFlow.Samples.Notepad.Secure.Externals.Storages
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
