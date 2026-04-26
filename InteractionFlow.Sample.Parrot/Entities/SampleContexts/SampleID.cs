namespace InteractionFlow.Samples.Parrot.Entities.SampleContexts
{
    internal readonly struct SampleID(SampleMode sample)
    {
        public readonly SampleMode mode = sample;

        public override string ToString()
        {
            return Enum.GetName(mode) ?? "???";
        }
    }
}
