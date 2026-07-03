using InteractionFlow.Core.Entities;
using InteractionFlow.Standard.ExternalPorts.StoragePorts.Entries;
using System;
using System.Threading.Tasks;

namespace InteractionFlow.Standard.ExternalPorts.StoragePorts.PersistencePorts
{

    public static class IPersistencePortCastExtensions
    {
        private sealed class InstantEntryCast<TValue, TEntry>
        (
            Func<Result<TValue>, Result<TEntry>> getEntry,
            Func<Result<TEntry>, Result<TValue>> getValue
        )
            : IEntryCast<TValue, TEntry>
        {
            public Result<TEntry> GetEntry(Result<TValue> value)
            {
                return getEntry(value);
            }
            public Result<TValue> GetValue(Result<TEntry> entry)
            {
                return getValue(entry);
            }
        }

        private sealed class CastPersistent<TPersistentID, TValue, TEntry>(
            IPersistencePort<TPersistentID, TValue> controller,
            IEntryCast<TValue, TEntry> cast)
            : IPersistencePort<TPersistentID, TEntry>
        {
            public Task<Result> Delete(TPersistentID id)
            {
                return controller.Delete(id);
            }

            public Task<Result> Exist(TPersistentID id)
            {
                return controller.Exist(id);
            }

            public Task<Result<TPersistentID[]>> GetAllId()
            {
                return controller.GetAllId();
            }

            public async Task<Result<TEntry>> Load(TPersistentID id, Result<TEntry> oldEntry)
            {
                var oldValue = cast.GetValue(oldEntry);
                var value = await controller.Load(id, oldValue);
                return cast.GetEntry(value);
            }

            public async Task<Result> Save(TPersistentID id, Result<TEntry> entry)
            {
                var value = cast.GetValue(entry);
                return await controller.Save(id, value);
            }
        }

        public static IPersistencePort<TPersistentID, TEntry> Cast<TPersistentID, TValue, TEntry>(
            this IPersistencePort<TPersistentID, TValue> controller,
            IEntryCast<TValue, TEntry> cast)
        {
            return new CastPersistent<TPersistentID, TValue, TEntry>(
                controller, cast);
        }

        public static IPersistencePort<TPersistentID, TEntry> Cast<TPersistentID, TValue, TEntry>(
            this IPersistencePort<TPersistentID, TValue> controller,
            Func<Result<TValue>, Result<TEntry>> getEntry,
            Func<Result<TEntry>, Result<TValue>> getValue)
        {
            return new CastPersistent<TPersistentID, TValue, TEntry>(
                controller, new InstantEntryCast<TValue, TEntry>(getEntry, getValue));
        }
    }
}
