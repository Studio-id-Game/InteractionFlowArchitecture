namespace InteractionFlow.Standard.ExternalPorts.StoragePorts.Entries
{
    public class RefEntryCast<TValue> : EntryCast<TValue, RefEntry<TValue>>
    {
        public override RefEntry<TValue> NewEntry(TValue? value)
        {
            return new RefEntry<TValue>(value);
        }
    }
}
