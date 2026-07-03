namespace InteractionFlow.Standard.ExternalPorts.StoragePorts.Entries
{
    public static class RefEntryExtentions
    {
        public static TValue? GetRefValue<TValue>(this Entry<RefEntry<TValue>> nest)
        {
            return nest.Value == null ? default : nest.Value.Value;
        }

        public static void SetRefValue<TValue>(this Entry<RefEntry<TValue>> nest, TValue value)
        {
            if (nest.Value == null)
                return;

            nest.Value.Value = value;
        }
    }
}
