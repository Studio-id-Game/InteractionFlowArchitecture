using InteractionFlow.Core.Entities;
using InteractionFlow.Samples.Notepad.Core.Entities.Datas;
using InteractionFlow.Samples.Notepad.Core.Entities.Keys;
using InteractionFlow.Standard.ExternalPorts.StoragePorts.Entries;
using InteractionFlow.Standard.ExternalPorts.StoragePorts.PersistencePorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Samples.Notepad.Core.ExternalPorts.StoragePorts.Entries
{
    public class NotepadEntry(NotepadDataKey fileID, NotepadData value)
        : PersistentEntry<NotepadDataKey, NotepadData>(fileID, value)
    {
        public NotepadData NotepadData
        {
            get => Value!;
        }

        public async Task<Result> SaveIfChanged(IPersistencePort<NotepadDataKey, NotepadData> persistentController)
        {
            var data = Value;

            if (data == null)
            {
                try
                {
                    throw new InvalidOperationException("NotepadEntry.SaveIfChanged() => data == null");
                }
                catch (Exception e)
                {
                    return e;
                }
            }

            if (data.HasChenged)
            {
                var result = await Save(persistentController);
                data.ChangeSaved();
                return result;
            }
            else
            {
                return Result.Success;
            }
        }
    }
}
