using InteractionFlow.Core.Entities;
using System;

namespace InteractionFlow.Standard.ExternalPorts.StoragePorts.Entries
{
    public abstract class EntryCast<TValue, TEntry> : IEntryCast<TValue, TEntry>
        where TEntry : Entry<TValue>
    {
        public Result<TEntry> GetEntry(Result<TValue> value)
        {
            return value.Then(v => NewEntry(v).AsResult());
        }

        public Result<TValue> GetValue(Result<TEntry> entry)
        {
            return entry.Then(e =>
            {
                if (e.Value == null)
                    return new NullReferenceException();
                else
                    return e.Value.AsResult();
            });
        }

        public abstract TEntry NewEntry(TValue? baseValue);
    }
}
