namespace InteractionFlow.Standard.ExternalPorts.StoragePorts.Entries
{
    public class RefEntry<TValue>(TValue? value) : Entry<TValue>(value)
    {
        public new TValue? Value
        {
            get => base.Value;
            set => base.Value = value;
        }
    }
}
