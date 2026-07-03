using InteractionFlow.Core.Entities;

namespace InteractionFlow.Standard.ExternalPorts.StoragePorts.Entries
{
    public interface IEntryCast<TValue, TEntry>
    {
        Result<TEntry> GetEntry(Result<TValue> value);

        Result<TValue> GetValue(Result<TEntry> entry);
    }

}
