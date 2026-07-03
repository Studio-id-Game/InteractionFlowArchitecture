using InteractionFlow.Core.Entities;

namespace InteractionFlow.Standard.ExternalPorts.StoragePorts.Entries
{
    public abstract class EntryCast<TValue, TEntry> : IEntryCast<TValue, TEntry>
        where TEntry : Entry<TValue>
    {
        public Result<TEntry> GetEntry(Result<TValue> value)
        {
            if (value)
            {
                return NewEntry(value.Value);
            }
            else
            {
                return value.Exception!;
            }
        }

        public Result<TValue> GetValue(Result<TEntry> entry)
        {
            if (entry && entry.Value != null && entry.Value.Value != null)
            {
                return entry.Value.Value;
            }
            else
            {
                return entry.Exception!;
            }
        }

        public abstract TEntry NewEntry(TValue? baseValue);
    }
}
