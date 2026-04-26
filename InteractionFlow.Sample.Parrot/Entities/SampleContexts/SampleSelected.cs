namespace InteractionFlow.Samples.Parrot.Entities.SampleContexts
{
    internal readonly struct SampleSelected(SampleID sample)
    {
        public readonly SampleID id = sample;

        public override string ToString()
        {
            return id.ToString();
        }
    }
}