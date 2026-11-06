namespace InteractionFlow.Samples.Parrot.Entities
{
    // One menu selection; the SystemFlow creates a fresh instance on each visit.
    internal sealed class SampleSelection
    {
        public SampleMode Mode { get; set; }
        public bool ExitRequested { get; set; }
    }

    internal sealed record ParrotGreeting(string Text);
}
