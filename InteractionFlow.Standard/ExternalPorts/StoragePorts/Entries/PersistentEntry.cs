using InteractionFlow.Core.Entities;
using InteractionFlow.Standard.ExternalPorts.StoragePorts.PersistencePorts;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.ExternalPorts.StoragePorts.Entries
{
    public class PersistentEntry<TPersistentId, TValue>(TPersistentId fileID, TValue? value) : Entry<TValue>(value)
    {
        public TPersistentId FileID => fileID;

        public async Task<Result> Save(IPersistencePort<TPersistentId, TValue> fileController)
        {
            try
            {
                if (Value == null)
                {
                    try
                    {
                        throw new InvalidOperationException("PersistentEntry.Save() => Value is null");
                    }
                    catch (Exception e)
                    {
                        return await fileController.Save(fileID, e);
                    }
                }
                else
                {
                    return await fileController.Save(fileID, Value);
                }
            }
            catch (Exception e)
            {
                return e;
            }
        }

        public async Task<Result<TValue>> Load(IPersistencePort<TPersistentId, TValue> fileController)
        {
            try
            {
                if (Value == null)
                {
                    try
                    {
                        throw new InvalidOperationException("PersistentEntry.Load() => Value is null");
                    }
                    catch (Exception e)
                    {
                        return await fileController.Load(fileID, e);
                    }
                }
                else
                {
                    return await fileController.Load(fileID, Value);
                }
            }
            catch (Exception e)
            {
                return e;
            }
        }
    }
}
